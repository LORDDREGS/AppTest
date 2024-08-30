using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Настройка MongoDB
var mongoClient = new MongoClient("mongodb://localhost:27017");
var database = mongoClient.GetDatabase("ManageApp");
var usersCollection = database.GetCollection<User>("users");
var plantsCollection = database.GetCollection<Plant>("plants");
var departmentsCollection = database.GetCollection<Department>("departments");
var positionsCollection = database.GetCollection<Position>("positions");

var countersCollection = database.GetCollection<BsonDocument>("counters");

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Пользователи
app.MapGet("/api/users", async () =>
{
    try
    {
        var users = await usersCollection.Find(_ => true).ToListAsync();

        var plants = await plantsCollection.Find(_ => true).ToListAsync();
        var departments = await departmentsCollection.Find(_ => true).ToListAsync();
        var positions = await positionsCollection.Find(_ => true).ToListAsync();

        var result = users.Select(user => new
        {
            user.Id,
            user.Email,
            user.LastName,
            user.FirstName,
            user.MiddleName,
            user.Password,
            user.CreatedDate,
            user.Roles,
            PlantName = plants.FirstOrDefault(p => p.Id == user.Plant)?.Name,
            DepartmentName = departments.FirstOrDefault(d => d.Id == user.Department)?.Name,
            PositionName = positions.FirstOrDefault(pos => pos.Id == user.Position)?.Name
        });

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.Problem("Internal Server Error");
    }
});


app.MapGet("/api/users/{id}", async (string id) =>
{
    try
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return Results.BadRequest(new { message = "Invalid user ID." });
        }

        var user = await usersCollection.Find(u => u.Id == id).FirstOrDefaultAsync();
        return user is null ? Results.NotFound(new { message = "User not found." }) : Results.Ok(user);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.Problem("Internal Server Error");
    }
});

app.MapPost("/api/users", async (User user) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(user.Plant) ||
            string.IsNullOrWhiteSpace(user.Department) ||
            string.IsNullOrWhiteSpace(user.Position))
        {
            return Results.BadRequest(new { message = "Invalid ObjectId values." });
        }

        user.Id = ObjectId.GenerateNewId().ToString(); // Генерация нового ID
        user.CreatedDate = DateTime.UtcNow; // Установка даты создания
        await usersCollection.InsertOneAsync(user);
        return Results.Created($"/api/users/{user.Id}", user);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.Problem("Internal Server Error");
    }
});

app.MapPut("/api/users/{id}", async (string id, User updatedUser) =>
{
    try
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return Results.BadRequest(new { message = "Invalid user ID." });
        }

        var filter = Builders<User>.Filter.Eq(u => u.Id, id);
        var update = Builders<User>.Update
            .Set(u => u.FirstName, updatedUser.FirstName)
            .Set(u => u.LastName, updatedUser.LastName)
            .Set(u => u.MiddleName, updatedUser.MiddleName)
            .Set(u => u.Email, updatedUser.Email)
            .Set(u => u.Roles, updatedUser.Roles)
            .Set(u => u.Password, updatedUser.Password)
            .Set(u => u.Plant, updatedUser.Plant)
            .Set(u => u.Department, updatedUser.Department)
            .Set(u => u.Position, updatedUser.Position);

        var options = new FindOneAndUpdateOptions<User> { ReturnDocument = ReturnDocument.After };

        var result = await usersCollection.FindOneAndUpdateAsync(filter, update, options);
        
        return result is null ? Results.NotFound(new { message = "User not found." }) : Results.Ok(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.Problem("Internal Server Error");
    }
});

app.MapDelete("/api/users/{id}", async (string id) =>
{
    try
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return Results.BadRequest(new { message = "Invalid user ID." });
        }

        var result = await usersCollection.FindOneAndDeleteAsync(u => u.Id == id);
        return result is null ? Results.NotFound(new { message = "User not found." }) : Results.Ok(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.Problem("Internal Server Error");
    }
});

// Заводы (Plants)
app.MapGet("/api/plants", async () =>
{
    try
    {
        var plants = await plantsCollection.Find(_ => true).ToListAsync();
        return Results.Ok(plants);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.Problem("Internal Server Error");
    }
});

app.MapGet("/api/plants/{id}", async (string id) =>
{
    try
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return Results.BadRequest(new { message = "Invalid plant ID." });
        }

        var plant = await plantsCollection.Find(p => p.Id == id).FirstOrDefaultAsync();
        return plant is null ? Results.NotFound(new { message = "Plant not found." }) : Results.Ok(plant);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.Problem("Internal Server Error");
    }
});

app.MapPost("/api/plants", async (Plant plant) =>
{
    try
    {
        plant.Id = ObjectId.GenerateNewId().ToString(); // Генерация нового ID
        await plantsCollection.InsertOneAsync(plant);
        return Results.Created($"/api/plants/{plant.Id}", plant);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.Problem("Internal Server Error");
    }
});

app.MapPut("/api/plants/{id}", async (string id, Plant updatedPlant) =>
{
    try
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return Results.BadRequest(new { message = "Invalid plant ID." });
        }

        var filter = Builders<Plant>.Filter.Eq(p => p.Id, id);
        var update = Builders<Plant>.Update
            .Set(p => p.Name, updatedPlant.Name)
            .Set(p => p.ShortName, updatedPlant.ShortName);

        var options = new FindOneAndUpdateOptions<Plant> { ReturnDocument = ReturnDocument.After };

        var result = await plantsCollection.FindOneAndUpdateAsync(filter, update, options);
        
        return result is null ? Results.NotFound(new { message = "Plant not found." }) : Results.Ok(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.Problem("Internal Server Error");
    }
});

app.MapDelete("/api/plants/{id}", async (string id) =>
{
    try
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return Results.BadRequest(new { message = "Invalid plant ID." });
        }

        var result = await plantsCollection.FindOneAndDeleteAsync(p => p.Id == id);
        return result is null ? Results.NotFound(new { message = "Plant not found." }) : Results.Ok(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.Problem("Internal Server Error");
    }
});

// Департаменты
app.MapGet("/api/departments", async () =>
{
    try
    {
        var departments = await departmentsCollection.Find(_ => true).ToListAsync();
        return Results.Ok(departments);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.Problem("Internal Server Error");
    }
});

app.MapGet("/api/departments/{id}", async (string id) =>
{
    try
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return Results.BadRequest(new { message = "Invalid department ID." });
        }

        var department = await departmentsCollection.Find(d => d.Id == id).FirstOrDefaultAsync();
        return department is null ? Results.NotFound(new { message = "Department not found." }) : Results.Ok(department);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.Problem("Internal Server Error");
    }
});

app.MapPost("/api/departments", async (Department department) =>
{
    try
    {
        department.Id = ObjectId.GenerateNewId().ToString(); // Генерация нового ID
        await departmentsCollection.InsertOneAsync(department);
        return Results.Created($"/api/departments/{department.Id}", department);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.Problem("Internal Server Error");
    }
});

app.MapPut("/api/departments/{id}", async (string id, Department updatedDepartment) =>
{
    try
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return Results.BadRequest(new { message = "Invalid department ID." });
        }

        var filter = Builders<Department>.Filter.Eq(d => d.Id, id);
        var update = Builders<Department>.Update
            .Set(d => d.Name, updatedDepartment.Name)
            .Set(d => d.ShortName, updatedDepartment.ShortName)
            .Set(d => d.Plant, updatedDepartment.Plant)
            .Set(d => d.IsAuditor, updatedDepartment.IsAuditor);

        var options = new FindOneAndUpdateOptions<Department> { ReturnDocument = ReturnDocument.After };

        var result = await departmentsCollection.FindOneAndUpdateAsync(filter, update, options);

        return result is null ? Results.NotFound(new { message = "Department not found." }) : Results.Ok(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.Problem("Internal Server Error");
    }
});

app.MapDelete("/api/departments/{id}", async (string id) =>
{
    try
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return Results.BadRequest(new { message = "Invalid department ID." });
        }

        var result = await departmentsCollection.FindOneAndDeleteAsync(d => d.Id == id);
        return result is null ? Results.NotFound(new { message = "Department not found." }) : Results.Ok(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.Problem("Internal Server Error");
    }
});

// Позиции
// Получение всех позиций
app.MapGet("/api/positions", async () =>
{
    try
    {
        var positions = await positionsCollection.Find(_ => true).ToListAsync();
        return Results.Ok(positions);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.Problem("Internal Server Error");
    }
});

// Получение позиции по ID
app.MapGet("/api/positions/{id}", async (string id) =>
{
    try
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return Results.BadRequest(new { message = "Invalid position ID." });
        }

        var position = await positionsCollection.Find(p => p.Id == id).FirstOrDefaultAsync();
        return position is null ? Results.NotFound(new { message = "Position not found." }) : Results.Ok(position);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.Problem("Internal Server Error");
    }
});

// Создание новой позиции
app.MapPost("/api/positions", async (Position position) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(position.Plant) || string.IsNullOrWhiteSpace(position.Department))
        {
            return Results.BadRequest(new { message = "Invalid ObjectId values." });
        }

        position.Id = ObjectId.GenerateNewId().ToString(); // Генерация нового ID
        await positionsCollection.InsertOneAsync(position);
        return Results.Created($"/api/positions/{position.Id}", position);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.Problem("Internal Server Error");
    }
});

// Обновление позиции
app.MapPut("/api/positions/{id}", async (string id, Position updatedPosition) =>
{
    try
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return Results.BadRequest(new { message = "Invalid position ID." });
        }

        var filter = Builders<Position>.Filter.Eq(p => p.Id, id);
        var update = Builders<Position>.Update
            .Set(p => p.Name, updatedPosition.Name)
            .Set(p => p.ShortName, updatedPosition.ShortName)
            .Set(p => p.Plant, updatedPosition.Plant)
            .Set(p => p.Department, updatedPosition.Department);

        var options = new FindOneAndUpdateOptions<Position> { ReturnDocument = ReturnDocument.After };

        var result = await positionsCollection.FindOneAndUpdateAsync(filter, update, options);

        return result is null ? Results.NotFound(new { message = "Position not found." }) : Results.Ok(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.Problem("Internal Server Error");
    }
});

// Удаление позиции
app.MapDelete("/api/positions/{id}", async (string id) =>
{
    try
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return Results.BadRequest(new { message = "Invalid position ID." });
        }

        var result = await positionsCollection.FindOneAndDeleteAsync(p => p.Id == id);
        return result is null ? Results.NotFound(new { message = "Position not found." }) : Results.Ok(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.Problem("Internal Server Error");
    }
});


// Запуск приложения
app.Run();

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = "";
 
    [BsonRepresentation(BsonType.ObjectId)]
    public string Plant { get; set; } = ""; // Ссылка на завод
 
    [BsonRepresentation(BsonType.ObjectId)]
    public string Department { get; set; }  = "";// Ссылка на подразделение
 // почему у тебя они указаны как строки?
    [BsonRepresentation(BsonType.ObjectId)]
    public string Position { get; set; }  = "";// Ссылка на должность
 
    public string Email { get; set; } = string.Empty; // Email пользователя
    public string LastName { get; set; } = string.Empty; // Фамилия
    public string FirstName { get; set; } = string.Empty; // Имя
    public string MiddleName { get; set; } = string.Empty; // Отчество
    public string Password { get; set; } = string.Empty; // Пароль (хранить зашифрованным!)
    public DateTime CreatedDate { get; set; } // Дата создания пользователя
    public List<string> Roles { get; set; } = new List<string>(); // Роли пользователя
}

public class Department
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = "";

    public string Name { get; set; } = string.Empty; // Название департамента
    public string ShortName { get; set; } = string.Empty; // Краткое название департамента

    [BsonRepresentation(BsonType.ObjectId)]
    public string Plant { get; set; } = ""; // Ссылка на завод
    
    public bool IsAuditor { get; set; } = false; // Флаг, является ли департамент аудитором
}

public class Position
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = "";

    [BsonRepresentation(BsonType.ObjectId)]
    public string Plant { get; set; } = ""; // Ссылка на завод

    [BsonRepresentation(BsonType.ObjectId)]
    public string Department { get; set; } = ""; // Ссылка на департамент

    public string Name { get; set; } = string.Empty; // Название должности
    public string ShortName { get; set; } = string.Empty; // Краткое название должности
}


public class Plant
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = "";

    public string Name { get; set; } = string.Empty; // Название завода
    public string ShortName { get; set; } = string.Empty; // Краткое название завода
}

public static class Utils
{
    public static async Task<int> GetNextSequenceValue(string sequenceName, IMongoCollection<BsonDocument> countersCollection)
    {
        try
        {
            var filter = Builders<BsonDocument>.Filter.Eq("id", sequenceName);
            var update = Builders<BsonDocument>.Update.Inc("sequence_value", 1);
            var options = new FindOneAndUpdateOptions<BsonDocument>
            {
                ReturnDocument = ReturnDocument.After

            };

            var result = await countersCollection.FindOneAndUpdateAsync(filter, update, options);

            return result != null ? result["sequence_value"].AsInt32 : 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw new Exception("Error occurred while getting next sequence value.", ex);
        }
    }
}
