using StaticMapper.Demo.Dtos;
using StaticMapper.Demo.Entities;

namespace StaticMapper.Demo
{
    public class DemoProfile : Profile
    {
        public DemoProfile() : base("DemoMapper")
        {
            CreateMap<User, UserInputDto>();
        }
    }
}