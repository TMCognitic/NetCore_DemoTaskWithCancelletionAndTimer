using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace DemoTimer
{
    internal class Program
    {        
        private static Dictionary<Tournoi, CancellationTokenSource> tokens = new Dictionary<Tournoi, CancellationTokenSource>();
        private static Dictionary<Tournoi, Task> threads = new Dictionary<Tournoi, Task>();
        private static ObservableCollection<Tournoi> tournois = new ObservableCollection<Tournoi>();


        static void Main(string[] args)
        {
            tournois.CollectionChanged += OnCollectionChange;

            Tournoi tournoi = new Tournoi("Tournoi 1", new DateTime(2022, 10, 26, 15, 08, 00));

            tournois.Add(tournoi);
            tournois.Add(new Tournoi("Tournoi 2", new DateTime(2022, 10, 26, 15, 08, 00)));

            Task.Delay(5000).Wait();
            tournois.Remove(tournoi);
            Console.ReadLine();
        }

        private static void OnCollectionChange(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == NotifyCollectionChangedAction.Add)
            {
                Tournoi? tournoiAdded = (Tournoi?)e.NewItems?[0];

                if(tournoiAdded is not null)
                {
                    TimeSpan timeSpan = tournoiAdded.Start - DateTime.Now;
                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                    cancellationTokenSource.Token.Register(() =>
                    {
                        tournoiAdded.Cancel();
                    });
                    tokens.Add(tournoiAdded, cancellationTokenSource);
                    threads.Add(tournoiAdded, Task.Run(() =>
                    {
                        Task.Delay((int)timeSpan.TotalMilliseconds).Wait(cancellationTokenSource.Token);
                        if (!cancellationTokenSource.IsCancellationRequested)
                        {
                            tournoiAdded.Launch();
                        }                        
                    }, cancellationTokenSource.Token)
                        .ContinueWith((task) =>
                        {
                            if (task.IsCompleted)
                            {
                                tokens.Remove(tournoiAdded);
                                threads.Remove(tournoiAdded);
                                tournois.Remove(tournoiAdded);
                            }
                        }));                    
                }
            }
            if(e.Action == NotifyCollectionChangedAction.Remove)
            {
                Tournoi? tournoiRemoved = (Tournoi?)e.OldItems?[0];
                if (tournoiRemoved is not null && tokens.ContainsKey(tournoiRemoved) && tokens[tournoiRemoved].Token.CanBeCanceled)
                {
                    tokens[tournoiRemoved].Cancel();
                    tokens.Remove(tournoiRemoved);
                    threads.Remove(tournoiRemoved);
                }
            }
        }
    }

    class Tournoi
    {
        public DateTime Start { get; init; }
        public string Nom { get; init; }

        public Tournoi(string nom, DateTime start)
        {
            if (start <= DateTime.Now)
                throw new ArgumentException("Le lancemant d'un tournoi doit être dans le futur...");

            Nom = nom;
            Start = start;
        }

        public void Launch()
        {
            DateTime now = DateTime.Now;
            Console.WriteLine($"Nous sommes le {now:yyyy MM dd} et il est {now:HH:mm}, le tournoi '{Nom}' commence...");
        }

        public void Cancel()
        {
            Console.WriteLine($"Tournoi '{Nom}' est annulé...");
        }
    }
}