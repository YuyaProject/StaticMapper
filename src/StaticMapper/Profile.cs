using System.Linq.Expressions;

namespace StaticMapper;

public abstract class Profile
{
    protected Profile(string mappingName)
    { }

    protected IMapperConfiguration<TSource, TDestination> CreateMap<TSource, TDestination>()
        => new MapperConfiguration<TSource, TDestination>();
}

public interface IMapperConfiguration<TSource, TDestination>
{
}

internal class MapperConfiguration<TSource, TDestination> : IMapperConfiguration<TSource, TDestination>
{ }

public interface IMapper
{ }