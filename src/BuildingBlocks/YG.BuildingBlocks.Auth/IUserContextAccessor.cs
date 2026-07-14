namespace YG.BuildingBlocks.Auth;

public interface IUserContextAccessor
{
    IUserContext Current { get; }
}