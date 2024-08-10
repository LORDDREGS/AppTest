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
var plantsCollection = database.GetCollection<Plant>("plants"); // Коллекция для заводов
var countersCollection = database.GetCollection<BsonDocument>("counters");

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Получение всех пользователей
app.MapGet("/api/users", async () =>
{
    var users = await usersCollection.Find(_ => true).ToListAsync();
    return Results.Ok(users);
});

// Получение одного пользователя по ID
app.MapGet("/api/users/{id}", async (string id) =>
{
    if (!ObjectId.TryParse(id, out var objectId))
    {
        return Results.BadRequest(new { message = "Invalid user ID." });
    }

    var user = await usersCollection.Find(u => u.Id == id).FirstOrDefaultAsync();
    return user is null ? Results.NotFound(new { message = "User not found." }) : Results.Ok(user);
});

// Создание нового пользователя
app.MapPost("/api/users", async (User user) =>
{
    user.Id = ObjectId.GenerateNewId().ToString(); // Генерация нового ID
    user.CreatedDate = DateTime.UtcNow; // Установка даты создания
    await usersCollection.InsertOneAsync(user);
    return Results.Created($"/api/users/{user.Id}", user);
});

// Обновление пользователя по ID
app.MapPut("/api/users/{id}", async (string id, User updatedUser) =>
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
        .Set(u => u.PlantId, updatedUser.PlantId);

    var options = new FindOneAndUpdateOptions<User> { ReturnDocument = ReturnDocument.After };

    var result = await usersCollection.FindOneAndUpdateAsync(filter, update, options);
    
    return result is null ? Results.NotFound(new { message = "User not found." }) : Results.Ok(result);
});

// Удаление пользователя по ID
app.MapDelete("/api/users/{id}", async (string id) =>
{
    if (!ObjectId.TryParse(id, out var objectId))
    {
        return Results.BadRequest(new { message = "Invalid user ID." });
    }

    var result = await usersCollection.FindOneAndDeleteAsync(u => u.Id == id);
    return result is null ? Results.NotFound(new { message = "User not found." }) : Results.Ok(result);
});

// Получение всех заводов
app.MapGet("/api/plants", async () =>
{
    var plants = await plantsCollection.Find(_ => true).ToListAsync();
    return Results.Ok(plants);
});

// Получение одного завода по ID
app.MapGet("/api/plants/{id}", async (string id) =>
{
    if (!ObjectId.TryParse(id, out var objectId))
    {
        return Results.BadRequest(new { message = "Invalid plant ID." });
    }

    var plant = await plantsCollection.Find(p => p.Id == id).FirstOrDefaultAsync();
    return plant is null ? Results.NotFound(new { message = "Plant not found." }) : Results.Ok(plant);
});

// Создание нового завода
app.MapPost("/api/plants", async (Plant plant) =>
{
    plant.Id = ObjectId.GenerateNewId().ToString(); // Генерация нового ID
    await plantsCollection.InsertOneAsync(plant);
    return Results.Created($"/api/plants/{plant.Id}", plant);
});

// Обновление завода по ID
app.MapPut("/api/plants/{id}", async (string id, Plant updatedPlant) =>
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
});

// Удаление завода по ID
app.MapDelete("/api/plants/{id}", async (string id) =>
{
    if (!ObjectId.TryParse(id, out var objectId))
    {
        return Results.BadRequest(new { message = "Invalid plant ID." });
    }

    var result = await plantsCollection.FindOneAndDeleteAsync(p => p.Id == id);
    return result is null ? Results.NotFound(new { message = "Plant not found." }) : Results.Ok(result);
});

// Запуск приложения
app.Run();

public class User
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = "";

    [BsonRepresentation(BsonType.ObjectId)]
    public string PlantId { get; set; } = ""; // Ссылка на завод
    public int Department { get; set; } // Ссылка на подразделение
    public int Position { get; set; } // Ссылка на должность
    public string Email { get; set; } = string.Empty; // Email пользователя
    public string LastName { get; set; } = string.Empty; // Фамилия
    public string FirstName { get; set; } = string.Empty; // Имя
    public string MiddleName { get; set; } = string.Empty; // Отчество
    public string Password { get; set; } = string.Empty; // Пароль (хранить зашифрованным!)
    public DateTime CreatedDate { get; set; } // Дата создания пользователя
    public List<string> Roles { get; set; } = new List<string>(); // Роли пользователя
}

public class Plant
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = "";

    public string Name { get; set; } = string.Empty; // Название завода
    public string ShortName { get; set; } = string.Empty; // Краткое название завода
}

public static class UserUtils
{
    public static async Task<int> GetNextSequenceValue(string sequenceName, IMongoCollection<BsonDocument> countersCollection)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("_id", sequenceName);
        var update = Builders<BsonDocument>.Update.Inc("sequence_value", 1);
        var options = new FindOneAndUpdateOptions<BsonDocument>
        {
            ReturnDocument = ReturnDocument.After
        };

        var result = await countersCollection.FindOneAndUpdateAsync(filter, update, options);

        return result != null ? result["sequence_value"].AsInt32 : 1; 
    }
}

public static class PlantUtils
{
    public static async Task<int> GetNextSequenceValue(string sequenceName, IMongoCollection<BsonDocument> countersCollection)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("_id", sequenceName);
        var update = Builders<BsonDocument>.Update.Inc("sequence_value", 1);
        var options = new FindOneAndUpdateOptions<BsonDocument>
        {
            ReturnDocument = ReturnDocument.After
        };

        var result = await countersCollection.FindOneAndUpdateAsync(filter, update, options);

        return result != null ? result["sequence_value"].AsInt32 : 1; 
    }
}