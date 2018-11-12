﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Neutronium.Core.Infra;
using Neutronium.Example.ViewModel.Infra;
using Neutronium.MVVMComponents;
using Neutronium.MVVMComponents.Relay;

namespace Neutronium.Example.ViewModel
{
    public class Person : ViewModelBase
    {
        public Person(ICommand forTest=null)
        {
            Skills = new ObservableCollection<Skill>();

            TestCommand = forTest;
            Command = new RelayToogleCommand(DoCommand);
            RemoveSkill = new RelaySimpleCommand<Skill>(s=> this.Skills.Remove(s));
            ChangeSkill = new RelaySimpleCommand<Skill>(s => MainSkill = (this.Skills.Count>0)?this.Skills[0] : null);
            RemoveSkills = new RelaySimpleCommand<Skill>(s => Skills.Clear());
        }

        private void DoCommand()
        {
            Local = new Local() { City = "Paris", Region = "IDF" };
            Skills.Insert(0, new Skill() { Name = "Info", Type = "javascript" });
            Command.ShouldExecute = false;
        }

        private string _LastName;
        public string LastName 
        {
            get => _LastName;
            set => Set(ref _LastName, value, "LastName");
        }

        private string _Name;
        public string Name 
        {
            get => _Name;
            set => Set(ref _Name, value, "Name");
        }

        private DateTime? _BirthDay;
        public DateTime? BirthDay
        {
            get => _BirthDay;
            set => Set(ref _BirthDay, value, "BirthDay");
        }

        private PersonalState _PersonalState;
        public PersonalState PersonalState
        {
            get => _PersonalState;
            set => Set(ref _PersonalState, value, "PersonalState");
        }

        private Sex _Sex;
        public Sex Sex 
        {
            get => _Sex;
            set => Set(ref _Sex, value, "Sex");
        }


        private int _Count;
        public int Count 
        {
            get => _Count;
            set => Set(ref _Count, value, "Count");
        }

        private int _Age;
        public int Age 
        {
            get => _Age;
            set => Set(ref _Age, value, "Age");
        }

        private int? _ChildrenNumber;
        public int? ChildrenNumber
        {
            get => _ChildrenNumber;
            set => Set(ref _ChildrenNumber, value, "ChildrenNumber");
        }

        private Local _Local;
        public Local Local
        {
            get => _Local;
            set => Set(ref _Local, value, "Local");
        }

        private Skill _MainSkill;
        public Skill MainSkill
        {
            get => _MainSkill;
            set => Set(ref _MainSkill, value, "MainSkill");
        }

        public IEnumerable<PersonalState> States => EnumExtensions.GetEnums<PersonalState>();

        public IEnumerable<Sex> Sexes => EnumExtensions.GetEnums<Sex>();

        public IList<Skill> Skills { get; }

        public RelayToogleCommand Command { get; }

        public ICommand RemoveSkill { get; }

        public ICommand ChangeSkill { get; }

        public ICommand RemoveSkills { get; }

        public ICommand TestCommand { get; set; }

        public ISimpleCommand AddOneYear { get; set; }
    }
}
