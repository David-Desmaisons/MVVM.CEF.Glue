﻿using Neutronium.Example.ViewModel.Infra;

namespace Tests.Universal.HTMLBindingTests.Helper
{
    internal class SimpleViewModel : ViewModelBase 
    {
        private string _Name;
        public string Name 
        {
            get => _Name;
            set => Set(ref _Name, value, "Name");
        }
    }
}
