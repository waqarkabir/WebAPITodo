
using Microsoft.AspNetCore.Http.HttpResults;

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