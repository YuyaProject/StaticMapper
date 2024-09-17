using StaticMapper.Demo.Dtos;
using StaticMapper.Demo.Entities;

namespace StaticMapper.Demo;

public class DemoProfile : Profile
{
    public DemoProfile() : base("DemoMapper")
    {
        CreateMap<User, User>();
        CreateMap<UserInputDto, User>().WithReverse();
        CreateMap<UserInput2Dto, User>();
        CreateMap<User, UserOutput1Dto>();
        CreateMap<User, UserOutput2Dto>();
        CreateMap<User, UserOutput3Dto>();
    }
}

public delegate object? ConvertingFunc(object? source, object? target);

public abstract class MapperBase : IMapperBase
{
    private static readonly Dictionary<Type, Dictionary<Type, ConvertingFunc>> _converters = new();

    protected static void AddConverter<TSource, TDestination>(ConvertingFunc converter)
    {
        var sourceType = typeof(TSource);
        var destinationType = typeof(TDestination);
        if (_converters.TryGetValue(destinationType, out var converters))
        {
            converters.Add(sourceType, converter);
        }
        else
        {
            _converters.Add(destinationType, new() { { sourceType, converter } });
        }
    }

    public virtual TDestination? Map<TDestination>(object? source) where TDestination : class
    {
        if (source == null) return default;
        if (_converters.TryGetValue(typeof(TDestination), out var converters))
        {
            var st = source.GetType();
            if (converters.TryGetValue(st, out var converter))
            {
                return converter(source, null) as TDestination;
            }
        }
        throw new NotSupportedException($"Mapping from {source.GetType().Name} to {typeof(TDestination).Name} is not supported.");
    }

    public virtual TDestination? Map<TSource, TDestination>(TSource? source) where TSource : class where TDestination : class
    {
        if (source == null) return default;
        if (_converters.TryGetValue(typeof(TDestination), out var converters))
        {
            var st = typeof(TSource);
            if (converters.TryGetValue(st, out var converter))
            {
                return converter(source, null) as TDestination;
            }
        }
        throw new NotSupportedException($"Mapping from {source.GetType().Name} to {typeof(TDestination).Name} is not supported.");
    }

    public virtual TDestination? Map<TSource, TDestination>(TSource? source, TDestination? destination) where TSource : class where TDestination : class
    {
        if (source == null) return default;
        if (TryGetConverter(typeof(TSource), typeof(TDestination), out var converter))
        {
            return converter!(source, destination) as TDestination;
        }
        throw new NotSupportedException($"Mapping from {source.GetType().Name} to {typeof(TDestination).Name} is not supported.");
    }

    private static bool TryGetConverter(Type sourceType, Type destinationType, out ConvertingFunc? converter)
    {
        converter = null;
        return _converters.TryGetValue(destinationType, out var converters) && converters.TryGetValue(sourceType, out converter);
    }
}

public class Demo2Mapper : MapperBase, IDemo2Mapper
{
    static Demo2Mapper()
    {
        AddConverter<User, User>(Convert_User_User);
    }

    // Source: StaticMapper.Demo.Entities.User, Destination: StaticMapper.Demo.Entities.User
    public User? Map(User? source, User? destination) => MapToStaticMapperDemoEntitiesUser(source, destination);

    public User? MapToUser(User? source) => MapToStaticMapperDemoEntitiesUser(source, null);

    private static object? Convert_User_User(object? source, object? destination)
    {
        return source is not User s ? destination : MapToStaticMapperDemoEntitiesUser(s, destination as User);
    }

    public static User? MapToStaticMapperDemoEntitiesUser(User? source, User? destination)
    {
        if (source == null) return destination;
        destination ??= new User();

        destination.UserName = source.UserName;
        destination.FirstName = source.FirstName;
        destination.LastName = source.LastName;
        destination.Email = source.Email;
        destination.Age = source.Age;

        return destination;
    }
}