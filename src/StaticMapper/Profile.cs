
using System.Linq.Expressions;

namespace StaticMapper;

public abstract class Profile
{
	public string MappingName { get; }

	protected Profile(string mappingName)
	{
		MappingName = mappingName;
	}

	protected IMapperConfiguration<TSource, TDestination> CreateMap<TSource, TDestination>()
		=> new MapperConfiguration<TSource, TDestination>();
}
