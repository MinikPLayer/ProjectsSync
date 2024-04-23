using System;
using System.Collections.Generic;
using ReactiveUI;

namespace ProjectsSyncAv.ViewModels
{
	public class ErrorWindowViewModel : ReactiveObject
	{
		private string _Title = "Unknown error";
		public string Title
		{
			get => _Title;
			set => this.RaiseAndSetIfChanged(ref _Title, value);
		}

		private string _Message = "Internal error. Report it to the administrator.";
		public string Message
		{
			get => _Message;
			set => this.RaiseAndSetIfChanged(ref _Message, value);
		}

		public ErrorWindowViewModel() { }

		public ErrorWindowViewModel(string title, string message)
		{
			this.Title = title;
			this.Message = message;
		}
	}
}