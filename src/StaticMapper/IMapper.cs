namespace StaticMapper;

public interface IMapper
{ }

#pragma warning disable S2326 // Unused type parameters should be removed
public interface IMapper<TProfile> : IMapper
#pragma warning restore S2326 // Unused type parameters should be removed
	where TProfile : Profile
{
}