using StaticMapper.Demo.Entities;

namespace StaticMapper.Demo
{
    public interface IDemo2Mapper : IMapperBase
    {
        User? Map(User? source, User? destination);

        User? MapToUser(User? source);
    }
}