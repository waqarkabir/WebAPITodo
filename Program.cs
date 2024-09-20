
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;

namespace WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Add Swagger services
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            //URL Redirection
            app.UseRewriter(new Microsoft.AspNetCore.Rewrite.RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));

            //Custom Middleware for Logging
            app.Use(async (context, next) => 
            {
                System.Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Started");
                await next(context);
                System.Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Finished");
            });

            // Enable Swagger UI only in development environment
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                c.RoutePrefix = string.Empty;  // Make Swagger available at the root URL
            });

            app.UseHttpsRedirection();


            var todos = new List<Todo>();

            app.MapGet("/todos", () => todos);

            app.MapGet("todos/{id}", Results<Ok<Todo>, NotFound> (int id) =>
            {
                var targetTodo = todos.SingleOrDefault(x => x.Id == id);
                return targetTodo is null
                    ? TypedResults.NotFound()
                    : TypedResults.Ok(targetTodo);
            });

            app.MapPost("/todos", (Todo task) =>
            {
                todos.Add(task);
                return TypedResults.Created("/todos/{id}", task);
            })
            .AddEndpointFilter(async (context, next) =>
            {
                var taskArgument = context.GetArgument<Todo>(0);
                var errors = new Dictionary<string, string[]>();
                if(taskArgument.DueDate < DateTime.UtcNow)
                {
                    errors.Add(nameof(Todo.DueDate) , ["Cannot have due date in the past."]);
                }
                if(taskArgument.IsCompleted)
                {
                    errors.Add(nameof(Todo.IsCompleted) , ["Cannot add completed todo."]);
                }

                foreach (var todo in todos)
                {
                    if (taskArgument.Id == todo.Id)
                    {
                        errors.Add(nameof(Todo.Id) , [$"Todo task with id: {todo.Id} already exists."]);
                    }
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);                    
                }

                return await next(context);
            });

            app.MapDelete("/todos/{id}", (int id) =>
            {
                todos.RemoveAll(t => id == t.Id);

                return TypedResults.NoContent();
            });

            app.Run();
        }
    }

    public record Todo(int Id, string Name, DateTime DueDate, bool IsCompleted);
}