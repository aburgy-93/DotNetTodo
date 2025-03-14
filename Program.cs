using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
// The following code adds the database context to the dependency 
// injection (DI) container and enables displaying database-related exceptions:
// The DI container provides access to the database context and other services.
builder.Services.AddDbContext<TodoDb>(opt => opt.UseInMemoryDatabase("TodoList"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// builder.Services.AddEndpointsApiExplorer();: Enables the API Explorer, which is 
// a service that provides metadata about the HTTP API. The API Explorer is used 
// by Swagger to generate the Swagger document.


// uilder.Services.AddOpenApiDocument(config => {...});: Adds the Swagger OpenAPI 
// document generator to the application services and configures it to provide more 
// information about the API, such as its title and version. For information on 
// providing more robust API details, see Get started with NSwag and ASP.NET Core


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "TodoAPI";
    config.Title = "TodoAPI v1";
    config.Version = "v1";
});
var app = builder.Build();

// The below code enables the Swagger middleware for serving the generated JSON 
// document and the Swagger UI. Swagger is only enabled in a development environment. 
// Enabling Swagger in a production environment could expose potentially sensitive details about the API's structure and implementation.
// if (app.Environment.IsDevelopment())

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentTitle = "TodoAPI";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/{documentName}/swagger.json";
        config.DocExpansion = "list";
    });
}

// adding urls 
// app.Urls.Add("http://localhost:3000");
// app.Urls.Add("http://localhost:4000");


var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";

RouteGroupBuilder todoItems = app.MapGroup("/todoitems");

app.Logger.LogInformation("The app started");

todoItems.MapGet("/", GetAllTodos);
todoItems.MapGet("/complete", GetCompleteTodos);
todoItems.MapGet("/{id}", GetTodo);
todoItems.MapPost("/", CreateTodo);
todoItems.MapPut("/{id}", UpdateTodo);
todoItems.MapDelete("/{id}", DeleteTodo);

app.Run();

static async Task<IResult> GetAllTodos(TodoDb db)
{
    return TypedResults.Ok(await db.Todos.Select(x => new TodoItemDTO(x)).ToArrayAsync());
}

static async Task<IResult> GetCompleteTodos(TodoDb db) {
    return TypedResults.Ok(await db.Todos.Where(t => t.IsComplete).Select(x => new TodoItemDTO(x)).ToListAsync());
}

static async Task<IResult> GetTodo(int id, TodoDb db)
{
    return await db.Todos.FindAsync(id)
        is Todo todo
            ? TypedResults.Ok(new TodoItemDTO(todo))
            : TypedResults.NotFound();
}

static async Task<IResult> CreateTodo(TodoItemDTO todoItemDTO, TodoDb db)
{
    var todoItem = new Todo
    {
        IsComplete = todoItemDTO.IsComplete,
        Name = todoItemDTO.Name
    };

    db.Todos.Add(todoItem);
    await db.SaveChangesAsync();

    todoItemDTO = new TodoItemDTO(todoItem);

    return TypedResults.Created($"/todoitems/{todoItem.Id}", todoItemDTO);
}

static async Task<IResult> UpdateTodo(int id, TodoItemDTO todoItemDTO, TodoDb db)
{
    var todo = await db.Todos.FindAsync(id);

    if (todo is null) return TypedResults.NotFound();

    todo.Name = todoItemDTO.Name;
    todo.IsComplete = todoItemDTO.IsComplete;

    await db.SaveChangesAsync();

    return TypedResults.NoContent();
}

static async Task<IResult> DeleteTodo(int id, TodoDb db)
{
    if (await db.Todos.FindAsync(id) is Todo todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    return TypedResults.NotFound();
}