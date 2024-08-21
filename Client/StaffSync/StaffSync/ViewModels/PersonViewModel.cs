using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace StaffSync.ViewModels
{
    public class PersonViewModel
    {
        public ObservableCollection<Person> People { get; set; }
        public ICommand GridCommand { get; }
        public ICommand CallCommand { get; }
        public PersonViewModel()
        {
            People = new ObservableCollection<Person>
            {
                new Person { Name = "Mustafa Beyazbulut", Title = "System Expert" },
                new Person { Name = "İsa Aydın", Title = "System Expert" },
                new Person { Name = "Oğuz Soysal", Title = "System Expert" },
                new Person { Name = "Oğuz Soysal", Title = "System Expert" },
                new Person { Name = "Oğuz Soysal", Title = "System Expert" },
                new Person { Name = "Oğuz Soysal", Title = "System Expert" },
                new Person { Name = "Oğuz Soysal", Title = "System Expert" },
                new Person { Name = "Oğuz Soysal", Title = "System Expert" },
                new Person { Name = "Oğuz Soysal", Title = "System Expert" },
            };

            GridCommand = new Command<Person>(OnGridTapped);
            CallCommand = new Command<Person>(OnCallClicked);
        }

        private void OnGridTapped(Person person)
        {
            // Grid tıklandığında ilgili personel bilgileriyle yapılacak işlemler
            Application.Current.MainPage.DisplayAlert("Grid Tıklama", $"Grid tıklandı! Person: {person.Name}", "Tamam");
        }

        private void OnCallClicked(Person person)
        {
            // Button tıklandığında ilgili personel bilgileriyle yapılacak işlemler
            Application.Current.MainPage.DisplayAlert("Arama Tıklama", $"Buton tıklandı! Person: {person.Name}", "Tamam");
        }

    }
    public class Person
    {
        public string Name { get; set; }
        public string Title { get; set; }
    }
}
