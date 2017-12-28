﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using gymlocator.Core;
using gymlocator.Rest.Models;
using gymlocator.Views;
using TK.CustomMap;
using Xamarin.Forms;
using Xamarin.Forms.Maps;

namespace gymlocator.ViewModels
{

    public class GymViewModel : ViewModelBase
    {
        private DataStore dataModel;
        private string currentFilter;

        public void FilterGyms(string newTextValue)
        {
            currentFilter = newTextValue.ToLower();
            FilterResults();
        }

        private TKCustomMap map;

        public GymViewModel(ContentPage page) : base(page)
        {
            dataModel = new DataStore();

        }

        public ICommand OpenGym => new Command((arg) =>
        {
            Gym gym = null;
            if (arg is TKCustomMapPin pin)
            {
                gym = Gyms.FirstOrDefault(d => d.Id == pin.ID);
            }
            else if (arg is Gym g)
            {
                gym = g;
            }
            Navigation.PushAsync(new GymDetailView(gym));
        });

        public ICommand DoRefresh => new Command(async () =>
        {
            var gyms = await dataModel.GetGymsAsync();
            PopulateGyms(gyms);
        });

        private IList<Gym> allGyms = new List<Gym>();

        public ObservableCollection<Gym> Gyms { get; set; } = new ObservableCollection<Gym>();
        public ObservableCollection<TKCustomMapPin> Pins { get; set; } = new ObservableCollection<TKCustomMapPin>();

        private bool hasRunInit = false;
        public async void Init(TKCustomMap map = null)
        {
            if (map != null)
                this.map = map;
            if (!hasRunInit)
            {
                hasRunInit = true;
                var gyms = await dataModel.GetGymsAsync();

                if (map != null)
                {
                    map.CustomPins = Pins;
                    map.CalloutClicked += (sender, e) => OpenGym.Execute(e.Value);
                }
                if (gyms != null && gyms.Any())
                {
                    PopulateGyms(gyms);
                }
            }
        }

        private void PopulateGyms(IList<Gym> gyms)
        {
            allGyms = gyms;
            Device.BeginInvokeOnMainThread(() =>
            {
                foreach (var gym in gyms)
                {
                    if (!allGyms.Any(d => d.Id == gym.Id))
                    {
                        allGyms.Add(gym);
                    }
                    if (gym.Location != null)
                    {
                        var pos = new Position(gym.Location.Lat, gym.Location.Lng);

                        if (!Pins.Any(d => d.ID == gym.Id))
                        {
                            Pins.Add(new TKCustomMapPin()
                            {
                                Position = pos,
                                ID = gym.Id,
                                IsVisible = true,
                                IsCalloutClickable = true,
                                Title = gym.Name,
                                ShowCallout = true,
                                Subtitle = gym.Address.StreetAddress
                            });
                        }
                    }
                }
            });
            FilterResults();
        }

        private void FilterResults()
        {
            var gymsToRemove = Gyms.Select(d => d.Id).ToList();
            foreach (var gym in allGyms)
            {
                if (Match(gym))
                {
                    if (gymsToRemove.Contains(gym.Id))
                    {
                        gymsToRemove.Remove(gym.Id);
                    }
                    else
                    {
                        Gyms.Add(gym);
                        var gymPin = Pins.FirstOrDefault();
                        if (gymPin != null)
                        {
                            gymPin.IsVisible = true;
                        }
                    }
                }
            }
            foreach(var toremove in gymsToRemove) {
                var gym = Gyms.FirstOrDefault(d => d.Id == toremove);
                var pinToRemove = Pins.FirstOrDefault(d => d.ID == toremove);
                if (gym != null)
                    Gyms.Remove(gym);
                if (pinToRemove != null)
                    pinToRemove.IsVisible = false;
            }
        }

        private bool Match(Gym gym)
        {
            if (string.IsNullOrWhiteSpace(currentFilter))
                return true;
            return (gym.Name.ToLower().Contains(currentFilter));
        }
    }
}