namespace Web.Testing;

internal sealed class TestAuthenticationStateProvider : AuthenticationStateProvider
{
	private AuthenticationState _state = new(new ClaimsPrincipal());

	public void SetUser(ClaimsPrincipal principal)
	{
		_state = new AuthenticationState(principal);
		NotifyAuthenticationStateChanged(Task.FromResult(_state));
	}

	public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
		Task.FromResult(_state);
}
