using System;
using System.Collections.Generic;
using ReactiveUI;

namespace ProjectsSyncAv.ViewModels
{
	public class SetupWindowViewModel : ReactiveObject
	{
		private bool _showCancelButton = false;
		public bool ShowCancelButton
		{
			get => _showCancelButton;
			set => this.RaiseAndSetIfChanged(ref _showCancelButton, value);
		}

		private string _UserEmail = "";
		public string UserEmail
		{
			get => _UserEmail;
			set => this.RaiseAndSetIfChanged(ref _UserEmail, value);
		}

		private string _RepoPath = "";
		public string RepoPath
		{
			get => _RepoPath;
			set => this.RaiseAndSetIfChanged(ref _RepoPath, value);
		}

		public SetupWindowViewModel(bool showCancelButton, AppConfig? config)
		{
			ShowCancelButton = showCancelButton;

			if(config.HasValue)
			{
				RepoPath = config.Value.RepoPath;
				UserEmail = config.Value.EmailAddress;
			}
		}

		public SetupWindowViewModel() { }
	}
}