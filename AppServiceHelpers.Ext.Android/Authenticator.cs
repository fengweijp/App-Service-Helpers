﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.WindowsAzure.MobileServices;
using Xamarin.Auth;
using AppServiceHelpers.Authentication;
using System.Linq;

namespace AppServiceHelpers
{
	public class Authenticator : IAuthenticator
	{
		private static readonly IAuthenticator instance = new Authenticator();
		internal static IAuthenticator Instance
		{
			get
			{
				return instance;
			}
		}

		public async Task<bool> LoginAsync(IMobileServiceClient client, MobileServiceAuthenticationProvider provider)
		{
			var success = false;

			try
			{
				if (CurrentPlatform.Context == null)
				{
					throw new Exception("You must call AppServiceHelpers.CurrentPlatform.Init(Context context) to authenticate users.");
				}

				var dictionary = new Dictionary<string, string>();
				switch (provider)
				{
					// Does not support refresh token concept with server-flow authentication.
					case MobileServiceAuthenticationProvider.Facebook:
					case MobileServiceAuthenticationProvider.Twitter:
                        break;
					// Supports refresh token concept, but all configuration is server-side.
					case MobileServiceAuthenticationProvider.MicrosoftAccount:
                    case MobileServiceAuthenticationProvider.WindowsAzureActiveDirectory:
                        break;
                    case MobileServiceAuthenticationProvider.Google:
						dictionary.Add("access_type", "offline");
						break;
				}

				var user = await client.LoginAsync(CurrentPlatform.Context, provider, dictionary);

				if (user != null)
				{
					var authenticationToken = client.CurrentUser.MobileServiceAuthenticationToken;
					var userId = client.CurrentUser.UserId;

					var keys = new Dictionary<string, string>
					{
						{ "userId", userId },
						{ "authenticationToken", authenticationToken },
						{ "identityProvider", provider.ToString() }
					};

					AccountStore.Create(CurrentPlatform.Context).Save(new Account(userId, keys), "appServiceHelpers");

					success = true;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error logging in: {ex.Message}");
			}

			return success;
		}

		public bool UserPreviouslyAuthenticated => string.IsNullOrEmpty(AccountStore.Create(CurrentPlatform.Context).FindAccountsForProvider("appServiceHelpers").FirstOrDefault()?.Properties["identityProvider"]);

		public MobileServiceAuthenticationProvider FindIdentityProvider()
		{
			var account = AccountStore.Create(CurrentPlatform.Context).FindAccountsForProvider("appServiceHelpers").First();

			var identityProvider = account.Properties["identityProvider"];
			switch (identityProvider)
			{
				case "Facebook":
					return MobileServiceAuthenticationProvider.Facebook;
				case "Twitter":
					return MobileServiceAuthenticationProvider.Twitter;
				case "Google":
					return MobileServiceAuthenticationProvider.Google;
				case "MicrosoftAccount":
					return MobileServiceAuthenticationProvider.MicrosoftAccount;
				case "WindowsAzureActiveDirectory":
					return MobileServiceAuthenticationProvider.WindowsAzureActiveDirectory;
			}

			return MobileServiceAuthenticationProvider.Google;
		}

		public Dictionary<string, string> LoadCachedUserCredentials()
		{
			if (AccountStore.Create(CurrentPlatform.Context).FindAccountsForProvider("appServiceHelpers").FirstOrDefault() != null)
			{
				return AccountStore.Create(CurrentPlatform.Context).FindAccountsForProvider("appServiceHelpers").FirstOrDefault()?.Properties;
			}
			else
			{
				return null;
			}
		}
	}
}