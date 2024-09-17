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