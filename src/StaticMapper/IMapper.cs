namespace StaticMapper;

public interface IMapper
{
	TDestination Map<TDestination>(object source)
		where TDestination : class;

	void Map(object source, object destination);
}

public interface IMapper<out TProfile> : IMapper
	where TProfile : Profile
{
	TProfile Profile { get; }
}