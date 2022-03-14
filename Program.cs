using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder();

// аутентификация с помощью куки
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => options.LoginPath = "/login");
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();   // добавление middleware аутентификации 
app.UseAuthorization();   // добавление middleware авторизации 

//Главная страница
app.MapGet("/", () => Results.LocalRedirect("/index"));
app.MapGet("/index", async (HttpContext context) =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync("wwwroot/index.html");
});

app.MapGet("/login", async (HttpContext context) =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync("wwwroot/login.html");
});

app.MapGet("/register", async (HttpContext context) =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync("wwwroot/register.html");
});

app.MapPost("/login", async (string? returnUrl, HttpContext context) =>
{
    // получаем из формы email и пароль
    var form = context.Request.Form;
    // если email и/или пароль не установлены, посылаем статусный код ошибки 400
    if (!form.ContainsKey("email") || !form.ContainsKey("password"))
        return Results.BadRequest("Email и/или пароль не установлены");
 
    string email = form["email"];
    string password = form["password"];

    // ищем пользователя 
    Person? person = DBworking.GetUserFromDB(email, password);

    // если пользователь не найден, отправляем статусный код
    if (person is null) return Results.BadRequest("Указанный Email не найден, Зарегестрируйтесь сперва!");

    var claims = new List<Claim> { new Claim(ClaimTypes.Name, person.email) };
    // создаем объект ClaimsIdentity
    ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "Cookies");
    // установка аутентификационных куки
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
    //await context.Response.SendFileAsync("wwwroot/login.html");
    return Results.Redirect(returnUrl??"/mainpage");
});

app.MapPost("/register", async (string? returnUrl, HttpContext context) =>
{
    // получаем из формы email и пароль
    var form = context.Request.Form;
    
    // если email и/или пароль не установлены, посылаем статусный код ошибки
    if (!form.ContainsKey("email") || !form.ContainsKey("password"))
        return Results.BadRequest("Email и/или пароль не установлены");

    string email = form["email"];
    string password = form["password"];
    string confpassword = form["confirmPassword"];

    // Проверка пароля и подтверждения
    if (password != confpassword)
        return Results.BadRequest("Пароль и подтверждение не совпадают!");

    //Проверка email пользователя на дублирование
    if (DBworking.CkeckUserInDB(email) == true)
        return Results.BadRequest("Указанный Email уже существует, придумайте другой");

    // ищем пользователя 
    Person? person = DBworking.GetUserFromDB(email, password);

    //Добавляем нового пользователя
    if (person is null)
        DBworking.SetNewUserInDB(email, password);

    var claims = new List<Claim> { new Claim(ClaimTypes.Name, email) };
    // создаем объект ClaimsIdentity
    ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "Cookies");
    // установка аутентификационных куки
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
    return Results.Redirect(returnUrl ?? "/mainpage");
});

app.MapPost("/addMark", async (string? returnUrl, HttpContext context) =>
{
    // получаем из формы текст заметки
    var form = context.Request.Form;

    var email = context.User.Identity.Name;
    string textmark = form["textmark"];

    if (textmark == "" )
        return Results.BadRequest("текст заметки пустой, так нельзя!");

    //Добавляем заметку
    DBworking.AddNewMarkInDB(email, textmark);

    return Results.Redirect(returnUrl ?? "/addMark");
});

//app.Map("/mainpage", [Authorize] () => Results.Redirect("/mainpage"));

//Выход из системы
app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapGet("/addMark", async (string? returnUrl, HttpContext context) =>
{
var cookies = context.Request.Cookies;
    context.Response.ContentType = "text/html; charset=utf-8";

    if (cookies.Count == 0)
    {
        await context.Response.SendFileAsync("wwwroot/index.html");
    }
    else 
    {
        await context.Response.SendFileAsync("wwwroot/addMark.html");
    }
});

app.MapGet("/listMarks", async (string? returnUrl, HttpContext context) =>
{
    var cookies = context.Request.Cookies;
    context.Response.ContentType = "text/html; charset=utf-8";

    if (cookies.Count == 0)
    {
        await context.Response.SendFileAsync("wwwroot/index.html");
    }
    else
    {
        await context.Response.SendFileAsync("wwwroot/listMarks.html");

    }
});

app.MapGet("/mainpage", async (string? returnUrl, HttpContext context) =>
{
    var cookies = context.Request.Cookies;
    context.Response.ContentType = "text/html; charset=utf-8";

    if (cookies.Count == 0)
    {
        await context.Response.SendFileAsync("wwwroot/index.html");
    }
    else
    {
        var form = context.Request.Form;
        var email = context.User.Identity.Name;

        int counter = 0;
        counter = DBworking.GetMarkCountFromDB(email); //Количество заметок
        await context.Response.SendFileAsync("wwwroot/mainpage.html");
    }
});

app.Run();

public class Mark //заметка
{
    public int id { get; set; }
    public string email { get; set; } = "";
    public string textmark { get; set; } = "";
    public string dttime { get; set; } = "";
}

public class Person
{
    public int id { get; set; }
    public string email { get; set; } = "";
    public string password { get; set; } = "";
}

public class ApplicationContext : DbContext
{
    public DbSet<Person> persons { get; set; }
    public DbSet<Mark> marks { get; set; }
    public ApplicationContext()
    {
        Database.EnsureCreated();
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=PersonsDB;Username=postgres;Password=Straus08!");
    }
}

public static class DBworking
{    public static bool CkeckUserInDB(string email)
    {
        using (ApplicationContext db = new ApplicationContext())
        {
            // получаем объект из бд
            var users = db.persons.ToList();
            Person? person = users.FirstOrDefault(p => p.email == email);
            if (person is null) return false;
            else return true;
        }
    }
    public static void SetNewUserInDB(string email, string password)
    {
        // добавление данных
        using (ApplicationContext db = new ApplicationContext())
        {
            int key = GetUserCountFromDB() + 1;
            // создаем объект User
            Person newuser = new Person { id = key, email = email, password = password };

            // добавляем в бд
            db.persons.AddRange(newuser);
            db.SaveChanges();
        }
    }
    public static Person GetUserFromDB(string email, string password)
    {
        using (ApplicationContext db = new ApplicationContext())
        {
            // получаем объект из бд
            var users = db.persons.ToList();
            Person? person = users.FirstOrDefault(p => p.email == email && p.password == password);
            return person;
        }
    }
    public static int GetUserCountFromDB()
    {
        using (ApplicationContext db = new ApplicationContext())
        {
            // получаем объект из бд
            var users = db.persons.ToList();
            return users.Count;
        }
    }

    public static void AddNewMarkInDB(string email, string textmark)
    {
        // добавление данных
        using (ApplicationContext db = new ApplicationContext())
        {
            int key = GetMarkCountFromDB(email) + 1;
            string dttime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            // создаем объект User
            Mark newmark = new Mark { id = key, email = email, textmark = textmark, dttime = dttime};

            // добавляем в бд
            db.marks.AddRange(newmark);
            db.SaveChanges();
        }
    }

    public static int GetMarkCountFromDB(string email) //количество добавленных заметок
    {
        using (ApplicationContext db = new ApplicationContext())
        {
            // получаем объект из бд
            var marks = db.marks.ToList();
            marks = (List<Mark>)marks.Select(p => p.email == email);
            return marks.Count;
        }
    }

    /*public static Mark GetMarkListFromDB(string email)
    {
        using (ApplicationContext db = new ApplicationContext())
        {
            // получаем объект из бд
            var marks = db.marklist.ToList();
            marks = (List<Mark>)marks.Select(p => p.email == email);
            return marks;
        }
    }*/
}