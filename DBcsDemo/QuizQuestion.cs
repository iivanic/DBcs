using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("questions")]
public class DBQuestion
{
    [Key]
    public int Id { get; set; }
    public int Difficulty { get; set; }
    [ForeignKey("DBCategory")]
    // property for refernced object
    public int CategoryId { get; set; }
    public DBCategory DBCategory { get; set; }
    [ForeignKey("DBAuthor")]
    // property for refernced object
    public int? AuthorId { get; set; }
    public DBAuthor? DBAuthor { get; set; }
    public string Question { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public string[] IncorrectAnswers { get; set; } = [];
    public bool IsDeleted { get; set; }
    // Collection of class that references via FK
    public List<DBQuizQuestion> QuizzesQuestions { get; set; } = new List<DBQuizQuestion>();
    // Not used by DBHelp directly
    public const string SelectText = "select * from questions;";
    public const string UpdateText = "update questions set difficulty=@difficulty, category_id=@category_id, author_id=@author_id, question=@question, correct_answer=@correct_answer, incorrect_answers=@incorrect_answers, is_deleted=@is_deleted where id=@id returning *;";
    public const string InsertText = "insert into questions (difficulty, category_id, author_id, question, correct_answer, incorrect_answers, is_deleted) values(@difficulty, @category_id, @author_id, @question, @correct_answer, @incorrect_answers, @is_deleted)  returning *;";
    public const string DeleteText = "delete from questions where id=@id;";

}

[Table("categories")]
public class DBCategory
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    // Collection of class that references via FK
    public List<DBQuestion> Questions { get; set; } = new List<DBQuestion>();
    // Not used by DBHelp directly
    public const string SelectText = "select * from categories;";
    public const string UpdateText = "update categories set name=@name, is_deleted=@is_deleted where id=@id returning *;";
    public const string InsertText = "insert into categories (name, is_deleted) values(@name, @is_deleted)  returning *;";
    public const string DeleteText = "delete from categories where id=@id;";

}

[Table("authors")]
public class DBAuthor
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    // Collection of class that references via FK
    public List<DBQuestion>? Questions { get; set; }
    // Collection of class that references via FK
    public List<DBQuiz>? Quizzes { get; set; }
    // Not used by DBHelp directly
    public const string SelectText = "select * from authors;";
    public const string UpdateText = "update authors set name=@name, is_deleted=@is_deleted where id=@id returning *;";
    public const string InsertText = "insert into authors (name, is_deleted) values(@name, @is_deleted)  returning *;";
    public const string DeleteText = "delete from authors where id=@id;";

}

[Table("quizzes")]
public class DBQuiz
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime AvalaibleFrom { get; set; }
    public DateTime? AvalaibiltyEnd { get; set; }
    [ForeignKey("DBAuthor")]
    // property for refernced object
    public int? AuthorId { get; set; }
    public DBAuthor? DBAuthor { get; set; }
    // Collection of class that references via FK
    public List<DBQuizQuestion> QuizzesQuestions { get; set; } = new List<DBQuizQuestion>();
    // Not used by DBHelp directly
    public const string SelectText = "select * from quizzes;";
    public const string UpdateText = "update quizzes set name=@name, avalaible_from=@avalaible_from, avalaibilty_end=@avalaibilty_end, author_id=@author_id where id=@id returning *;";
    public const string InsertText = "insert into quizzes (name, avalaible_from, avalaibilty_end, author_id) values(@name, @avalaible_from, @avalaibilty_end, @author_id)  returning *;";
    public const string DeleteText = "delete from quizzes where id=@id;";

}

[Table("quizzes_questions")]
public class DBQuizQuestion
{
    [Key]
    public int Id { get; set; }
    [ForeignKey("DBQuiz")]
    // property for refernced object
    public int QuizId { get; set; }
    public DBQuiz DBQuiz { get; set; }
    [ForeignKey("DBQuestion")]
    // property for refernced object
    public int QuestionId { get; set; }
    public DBQuestion DBQuestion { get; set; }
    // Not used by DBHelp directly
    public const string SelectText = "select * from quizzes_questions;";
    public const string UpdateText = "update quizzes_questions set quiz_id=@quiz_id, question_id=@question_id where id=@id returning *;";
    public const string InsertText = "insert into quizzes_questions (quiz_id, question_id) values(@quiz_id, @question_id)  returning *;";
    public const string DeleteText = "delete from quizzes_questions where id=@id;";

}