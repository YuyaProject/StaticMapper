namespace StaticMapper.Demo.Entities;

public class User : Entity
{
    public string UserName { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Email { get; set; }

    public int Age { get; set; }
}

public class Entity
{
    public int Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.Now;
}

public class Blog : Entity
{
    public string Title { get; set; }

    public string Content { get; set; }

    public int AuthorId { get; set; }

    public User? Author { get; set; }
}