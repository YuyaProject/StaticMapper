namespace StaticMapper;

public interface IMapperConfiguration<TSource, TDestination>
{
	void WithReverse();
}
