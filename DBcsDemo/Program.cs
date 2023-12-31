// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

var dbName = "db_helper_quiz";
// with postgres we can not change databases while connected
// so we must have two connection strings and reconnect
const string connString = @"Server=192.168.0.2;Port=5432;Username=pguser;Password=sa;database={DATABASE};";
var connStringDefaultDb = connString.Replace("{DATABASE}", "postgres"); ;
var connStringMyDb = connString.Replace("{DATABASE}", dbName);

//does our database already exists?
var dBcs = new DBcs.DBcs(connStringDefaultDb);
var o = await dBcs.RunScalarAsync(
    $"Select count(*) from pg_database where datname='{dbName}' and datistemplate = false;\n");

//if doesnt exists, create database
if (o != null)
{
    if ((long)o == 0)
    { 
        await dBcs.RunNonQueryAsync($"CREATE DATABASE {dbName};");
        Console.Write($"Database {dbName} created.");
    }
    else
    {
        await dBcs.RunNonQueryAsync($"DROP DATABASE IF EXISTS {dbName} WITH(FORCE);");
        Console.Write("Database deleted.");
        await dBcs.RunNonQueryAsync($"CREATE DATABASE {dbName};");
        Console.Write("Database created.");

    }
}
dBcs = new DBcs.DBcs(connStringMyDb);
//create schema
await dBcs.RunNonQueryAsync(
    File.OpenText(
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CreateTables.sql")
    ).ReadToEnd());
Console.Write(" Schema created.");
//seed data
await dBcs.RunNonQueryAsync(
    File.OpenText(
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InsertData.sql")
    ).ReadToEnd());
Console.Write(" Database seeded.");

// EXAMPLES:

//generate classes from quuery
var classes = await dBcs.GetClassCodeString(
    // Class represents one row, while List of classes
    // represents table. Thats why is good practice to
    // name classes in singular, and tables in plural
    //
    // Also it is recommended to add prefix to class names
    // that represents items from db.
    //  -makes you aware that this class represents data
    //   row in db
    //  -avoids name conflicts - in our quiz example, if 
    //   we name "Question" item from table questions,
    //   it will be in conflict with property "Question",
    //   Which represents text of the question.
    //
    new[]
    {
        "DBQuestion",
        "DBCategory",
        "DBAuthor",
        "DBQuiz",
        "DBQuizQuestion",
    },
    @"
        select * from questions;
        select * from categories;
        select * from authors;
        select * from quizzes;
        select * from quizzes_questions;

    ");
Console.WriteLine("");
Console.WriteLine($"****Code is ------------------------------------------{Environment.NewLine}{classes}{Environment.NewLine}");
Console.WriteLine($"***Code end -------------------------------------------{Environment.NewLine}{Environment.NewLine}");

//
Console.WriteLine($"{Environment.NewLine}***Load QuizCategory collection:{Environment.NewLine}");

var categories = await dBcs.RunQueryAsync<DBCategory>(
    "select * from categories;");

if (categories != null)
    foreach (var cat in categories)
    {
        Console.WriteLine($"{cat.Id} {cat.Name}");
    }

Console.WriteLine($"{Environment.NewLine}***Load single QuizCategory:{Environment.NewLine}");
var category = await dBcs.RunQuerySingleOrDefaultAsync<DBCategory>(
    "select * from categories where id=3;");

if (category != null)
    Console.WriteLine($"{category.Id} {category.Name}");


Console.WriteLine($"{Environment.NewLine}***Update single QuizCategory:{Environment.NewLine}");
category.Name = "Science & Computer science";
category = await dBcs.RunQuerySingleOrDefaultAsync<DBCategory>(
    "update public.categories set name=@name, is_deleted=@is_deleted where id=@id returning *;", category);
if (category != null)
    Console.WriteLine($"Name has now changed: {category.Id} {category.Name}");

Console.WriteLine($"{Environment.NewLine}***Update single QuizCategory if you used dBcs.GetClassCodeString to generate classes:{Environment.NewLine}");

category.Name = "Science";
category = await dBcs.RunQuerySingleOrDefaultAsync<DBCategory>(DBCategory.UpdateText, category);
if (category != null)
    Console.WriteLine($"Name has now changed: {category.Id} {category.Name}");

Console.WriteLine($"{Environment.NewLine}***Load QuizCategory collection without storing it in memory:{Environment.NewLine}");

var lastCategory = "";
await dBcs.RunQueryWithCallBackAsync<DBCategory>(
    "select * from categories;", (c =>
        {
            Console.WriteLine($"Soon will be forgotten: {c.Name}");
            // obviously variable scope is not changed
            lastCategory = c.Name;
        }));

Console.WriteLine($"{Environment.NewLine}***------------------------------------:lastCategory = {lastCategory}{Environment.NewLine}");

// or
Console.WriteLine($"{Environment.NewLine}***Load QuizQuestion collection:{Environment.NewLine}");

var questions = await dBcs.RunQueryAsync<DBQuestion>(
    "select * from questions;");

foreach (var question in questions)
{
    Console.WriteLine($"{question.Id} {question.Question} - Answer is: {question.CorrectAnswer}");
}


Console.WriteLine("End of simple part ------------------------------------------------------------------------------");

//fast load collection with 1 level childred object
//join all records, at the start of the query must be 
//Main object proeprties and query must be ordered by
//main object, and then child object

Console.WriteLine($"{Environment.NewLine}***Load QuizQuestion and QuizCategory:{Environment.NewLine}");

var qs = await dBcs.RunAndFillReferenceTypesAsync<DBQuestion>(
    @"
    select * 
    from 
        public.questions q
    inner join 
        public.categories c on q.category_id=c.id
    inner join 
        public.authors a on q.author_id=a.id
    where
        c.id=1
    order by
        q.id, c.id;
");

foreach (var question in qs)
{
    Console.WriteLine($"   [Category: {question.DBCategory?.Name}] [Author: {question.DBAuthor?.Name}] {question.Question} - Answer is: {question.CorrectAnswer}");
}

Console.WriteLine($"{Environment.NewLine}***Load QuizQuestion and QuizCategory Without storing it in memory:{Environment.NewLine}");

await dBcs.RunAndFillReferenceTypesWithCallbackAsync<DBQuestion>(
    @"
    select * 
    from 
        public.questions q
    inner join 
        public.categories c on q.category_id=c.id
    inner join 
        public.authors a on q.author_id=a.id
    where
        c.id=1
    order by
        q.id, c.id;
", (qq =>{Console.WriteLine($"Soon will be forgotten: [Category: {qq.DBCategory?.Name}] [Author: {qq.DBAuthor?.Name}] {qq.Question}");}));



Console.WriteLine($"{Environment.NewLine}***Load and Quiz Categories With List of Questions:{Environment.NewLine}");

var qs1 = await dBcs.RunAndFillReferenceTypesAsync<DBCategory>(
    @"
        Select
            *
        from
            public.categories c 
        inner join
            public.questions q on q.category_id=c.id
        order by
            c.id, q.id ;
");

foreach (var cat in qs1)
{
    Console.WriteLine($"   Questions in {cat.Name} category:");
    foreach (var question in cat.Questions)
    {
        Console.WriteLine($"      {question.Question} - Answer is: {question.CorrectAnswer}");
    }
}


Console.WriteLine($"{Environment.NewLine}***Load and Quiz Categories With List of Questions:{Environment.NewLine}");

await dBcs.RunAndFillReferenceTypesWithCallbackAsync<DBCategory>(
    @"
        Select
            *
        from
            public.categories c 
        inner join
            public.questions q on q.category_id=c.id
        order by
            c.id, q.id ;
", (qc => {
        Console.WriteLine($"Soon will be forgotten:    Questions in {qc.Name} category:");
        foreach (var q in qc.Questions)
        {
            Console.WriteLine($"Soon will be forgotten:       {q.Question} - Answer is: {q.CorrectAnswer}");
        }
    }));


Console.WriteLine($"{Environment.NewLine}***Load Authors and fill Quizzes and Questions:{Environment.NewLine}");

await dBcs.RunAndFillReferenceTypesWithCallbackAsync<DBAuthor>(
    @"
        select
	        *
        from 
	        authors a
        inner join 
	        quizzes qz on a.id = qz.author_id
        inner join 
	        questions q on a.id = q.author_id
        order by
	        a.id, qz.id, q.id;
",(a => {
       
            Console.WriteLine($"Soon will be forgotten:   Author Questions: {a.Name}");
            foreach (var question in a.Questions)
            {
                Console.WriteLine($"Soon will be forgotten:      {question.Question} - Answer is: {question.CorrectAnswer}");
            }
            Console.WriteLine($"Soon will be forgotten:   Author Quizzes: {a.Name}");
            foreach (var quiz in a.Quizzes)
            {
                Console.WriteLine($"Soon will be forgotten:      {quiz.Name}");
            }
      
    }));



Console.WriteLine($"{Environment.NewLine}***Load Authors and fill Quizzes and Questions:{Environment.NewLine}");

var qs2 = await dBcs.RunAndFillReferenceTypesAsync<DBAuthor>(
    @"
        select
	        *
        from 
	        authors a
        inner join 
	        quizzes qz on a.id = qz.author_id
        inner join 
	        questions q on a.id = q.author_id
        order by
	        a.id, qz.id, q.id;
");

foreach (var a in qs2)
{
    Console.WriteLine($"   Author Questions: {a.Name}");
    foreach (var question in a.Questions)
    {
        Console.WriteLine($"      {question.Question} - Answer is: {question.CorrectAnswer}");
    }
    Console.WriteLine($"   Author Quizzes: {a.Name}");
    foreach (var quiz in a.Quizzes)
    {
        Console.WriteLine($"      {quiz.Name}");
    }
}
Console.WriteLine();
Console.WriteLine("Generate DDL for postgres from classes:");
Console.WriteLine(
      dBcs.GetDDLCodeString(
        new [] {
            typeof(DBAuthor),
            typeof(DBCategory),
            typeof(DBQuestion)}, 
        new [] {
            "authors",
            "categories",
            "questions"}
             )
     );


dBcs = new DBcs.DBcs(connStringDefaultDb);

//await dBcs.RunNonQuery($"DROP DATABASE IF EXISTS {dbName} WITH(FORCE);");

Console.WriteLine();
//Console.WriteLine($"Database deleted.");
