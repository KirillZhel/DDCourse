namespace Api.Models
{
	public class RefreshTokenRequestModel
	{
		public string RefreshToken { get; set; }

		public RefreshTokenRequestModel(string refreshToken)
		{
			RefreshToken = refreshToken;
		}

	}
}
