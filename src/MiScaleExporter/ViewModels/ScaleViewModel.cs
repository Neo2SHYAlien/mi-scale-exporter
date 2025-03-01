﻿using MiScaleExporter.Models;
using MiScaleExporter.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Essentials;

namespace MiScaleExporter.ViewModels
{
    public class ScaleViewModel : BaseViewModel, IScaleViewModel
    {
        private readonly IScaleService _scaleService;
        private readonly ILogService _logService;

        public ScaleViewModel(IScaleService scaleService, ILogService logService)
        {
            _scaleService = scaleService;
            _logService = logService;
            this.LoadPreferences();

            Title = "Mi Scale Data";
            CancelCommand = new Command(OnCancel);
            ScanCommand = new Command(OnScan, ValidateScan);
        }

        private void LoadPreferences()
        {
            this._age = Preferences.Get(PreferencesKeys.UserAge, 25);
            this._height = Preferences.Get(PreferencesKeys.UserHeight, 170);
            this._sex = (Sex) Preferences.Get(PreferencesKeys.UserSex, (byte) Sex.Male);
            this._address = Preferences.Get(PreferencesKeys.MiScaleBluetoothAddress, string.Empty);
        }

        private void SavePrefences()
        {
            Preferences.Set(PreferencesKeys.UserAge, _age);
            Preferences.Set(PreferencesKeys.UserHeight, _height);
            Preferences.Set(PreferencesKeys.UserSex, (byte) _sex);
            Preferences.Set(PreferencesKeys.MiScaleBluetoothAddress, _address);
        }

        private async void OnScan()
        {
            this.SavePrefences();
            if (await GetLocationPermissionStatusAsync() != PermissionStatus.Granted)
            {
                await Application.Current.MainPage.DisplayAlert("Problem", "Permission to use Bluetooth is required to scan.",
                    "OK");
                return;
            }

            Scale scale = new Scale()
            {
                Address = Address,
            };
            ScanningLabel = string.Empty;
            this.IsBusy = true;
            var bc = await _scaleService.GetBodyCompositonAsync(scale,
                new User {Sex = _sex, Age = _age, Height = _height});

            this.IsBusy = false;
            if (bc is null || !bc.IsValid)
            {
                var msg = "Data could not be obtained. try again";
                await Application.Current.MainPage.DisplayAlert("Problem", msg,
                    "OK");
                _logService.LogError(msg);
                ScanningLabel = "Not found";
            }
            else
            {
                App.BodyComposition = bc;
                await Shell.Current.GoToAsync("///FormPage");
            }
        }

        private async Task<PermissionStatus> GetLocationPermissionStatusAsync()
        {
            var locationPermissionStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (locationPermissionStatus != PermissionStatus.Granted)
            {
                locationPermissionStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            return locationPermissionStatus;
        }

        private bool ValidateScan()
        {
            return !String.IsNullOrWhiteSpace(_address)
                                        && _height > 0 && _height < 220
                                        && _age > 0 && _age < 99;
        }

        public Command ScanCommand { get; }
        public Command CancelCommand { get; }

        private async void OnCancel()
        {
            await _scaleService.CancelSearchAsync();
            this.IsBusy = false;
        }
        public void SexRadioButtonChanged(object s, CheckedChangedEventArgs e)
        {
            var radio = s as RadioButton;
            this.Sex = radio.Value as string == "1" ? Models.Sex.Male : Models.Sex.Female;
        }

        

        private string _address;

        public string Address
        {
            get => _address;
            set
            {
                SetProperty(ref _address, value);
                ScanCommand?.ChangeCanExecute();
            }
        }

        private int _age;

        public string Age
        {
            get => _age.ToString();
            set
            {
                if (value is null) return;
                if (int.TryParse(value, out var result))
                {
                    SetProperty(ref _age, result);
                    ScanCommand?.ChangeCanExecute();
                }
            }
        }

        private int _height;

        public string Height
        {
            get => _height.ToString();
            set
            {
                if (value is null) return;
                if (int.TryParse(value, out var result))
                {
                    SetProperty(ref _height, result);
                    ScanCommand?.ChangeCanExecute();
                }
            }
        }

        private Sex _sex;

        public Sex Sex
        {
            get => _sex;
            set => SetProperty(ref _sex, value);
        }

        private string _scanningLabel;

        public string ScanningLabel
        {
            get => _scanningLabel;
            set => SetProperty(ref _scanningLabel, value);
        }
        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }
        
    }
}