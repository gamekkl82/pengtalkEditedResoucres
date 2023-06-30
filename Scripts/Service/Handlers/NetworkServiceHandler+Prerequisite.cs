using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public partial class NetworkServiceHandler
{
	[ServicePrerequisite]
	public async Task<MPServiceResult> CheckNetworkConnection(string contents)
	{
		if (Application.internetReachability == NetworkReachability.NotReachable)
			return MPServiceResult.Fail(MPServiceResultCode.NetworkNotConnected);

		return MPServiceResult.Success();
	}

	[ServicePrerequisite(1, except: NetworkServiceRequest.Login)]
	public async Task<MPServiceResult> RestoreSession(string contents)
	{
		if (!IsSessionValid)
		{
			if (!string.IsNullOrEmpty(loginContents))
			{
				return await MPServiceFacade.Network.Request(NetworkServiceRequest.Login, loginContents);
			}
			else
			{
				return MPServiceResult.Fail(MPServiceResultCode.InvalidUserSession);
			}
		}

		return MPServiceResult.Success();
	}

	[ServicePrerequisite(10, except: NetworkServiceRequest.Login)]
	public async Task<MPServiceResult> CheckUserSessionValidities(string contents)
	{
		if (!IsSessionValid)
			return MPServiceResult.Fail(MPServiceResultCode.InvalidUserSession);

		return MPServiceResult.Success();
	}
}
