namespace StaticMapper.Demo.Dtos;

public class UserInputDto
{
    public int Id { get; set; }

    public string UserName { get; set; }

    public string Email { get; set; }

    public int Age { get; set; }
}

public class UserInput2Dto
{
    public int Id { get; set; }

    public string UserName { get; set; }
}

public class UserOutput1Dto
{
    public int Id { get; set; }

    public string UserName { get; set; }

    public string Email { get; set; }

    public int Age { get; set; }
}

public class UserOutput2Dto
{
    public int Id { get; set; }

    public string UserName { get; set; }

    public string Email { get; set; }
    public string FullName { get; set; }
}

public class UserOutput3Dto
{
    public int Id { get; set; }

    public string UserName { get; set; }

    public string FullName { get; set; }
}