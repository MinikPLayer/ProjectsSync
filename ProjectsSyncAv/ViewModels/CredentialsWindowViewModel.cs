using System;
using System.Collections.Generic;
using ReactiveUI;

namespace ProjectsSyncAv.ViewModels
{
	public class CredentialsWindowViewModel : ReactiveObject
	{
		private string _url = "Unknown";
		public string Url
		{
			get => _url;
			set => this.RaiseAndSetIfChanged(ref _url, value);
		}

		private string _Login = "";
		public string Login
		{
			get => _Login;
			set => this.RaiseAndSetIfChanged(ref _Login, value);
		}

		private string _Password = "";
		public string Password
		{
			get => _Password;
			set => this.RaiseAndSetIfChanged(ref _Password, value);
		}
	}
}