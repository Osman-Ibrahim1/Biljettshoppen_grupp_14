using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace Biljettshopen
{
    public enum SeatType { Fällstol, Bänk }
    public enum SeatStatus { Ledig, Reserverad, Upptagen }

    public class Seat
    {
        public int Number { get; }
        public SeatType Type { get; }
        public SeatStatus Status { get; set; } = SeatStatus.Ledig;

        public Seat(int number, SeatType type)
        {
            Number = number;
            Type = type;
        }
    }

    public class Event
    {
        public string Name { get; }
        public DateTime EventDate { get; }
        public DateTime TicketReleaseDate { get; }
        public List<Seat> Seats { get; }
        public int AvailableTickets => Seats.Count(s => s.Status == SeatStatus.Ledig);

        public Event(string name, DateTime eventDate, DateTime ticketReleaseDate, int seatCount)
        {
            Name = name;
            EventDate = eventDate;
            TicketReleaseDate = ticketReleaseDate;
            Seats = new List<Seat>();

            for (int i = 1; i <= seatCount; i++)
            {
                SeatType type = i % 2 == 0 ? SeatType.Fällstol : SeatType.Bänk;
                Seats.Add(new Seat(i, type));
            }
        }

        public void ShowSeats()
        {
            Console.WriteLine("\nTillgängliga platser:");
            Console.WriteLine("---------------------------");
            foreach (var seat in Seats)
            {
                Console.WriteLine($"Plats {seat.Number.ToString("D2")}: Typ - {seat.Type}, Status - {seat.Status}");
            }
            Console.WriteLine("---------------------------");
        }

        public bool ReserveSelectedSeats(List<int> seatNumbers, Action<int> timeoutAction)
        {
            var selectedSeats = Seats.Where(s => seatNumbers.Contains(s.Number) && s.Status == SeatStatus.Ledig).ToList();

            if (selectedSeats.Count < seatNumbers.Count)
            {
                Console.WriteLine("En eller flera av de valda platserna är inte tillgängliga. Försök igen med andra platser.");
                return false;
            }

            foreach (var seat in selectedSeats)
            {
                seat.Status = SeatStatus.Reserverad;
                Console.WriteLine($"Plats {seat.Number} reserverad. Du har 10 minuter att slutföra köpet.");

                System.Timers.Timer timer = new System.Timers.Timer(600000); // 10 minuter
                timer.Elapsed += (sender, e) => timeoutAction(seat.Number);
                timer.AutoReset = false;
                timer.Start();
            }
            return true;
        }

        public void CompletePurchase(List<int> seatNumbers)
        {
            foreach (var seatNumber in seatNumbers)
            {
                var seat = Seats.FirstOrDefault(s => s.Number == seatNumber && s.Status == SeatStatus.Reserverad);
                if (seat != null)
                {
                    seat.Status = SeatStatus.Upptagen;
                    Console.WriteLine($"Köp slutfört för plats {seat.Number}.");
                }
                else
                {
                    Console.WriteLine($"Ingen reservation hittades för plats {seatNumber}.");
                }
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            DateTime startTime = DateTime.Now;

            List<Event> events = new List<Event>
            {
                new Event("Hallawinfest", new DateTime(2024, 11, 5), startTime.AddMinutes(1), 20),
                new Event("Sommarfest", new DateTime(2024, 11, 2), startTime.AddMinutes(1), 20),
                new Event("Studentfest", new DateTime(2024, 11, 1), startTime.AddMinutes(1), 20)
            };

            Console.WriteLine("Välkommen till Biljettshopen!");
            bool running = true;
            while (running)
            {
                Console.WriteLine("\nVälj ett alternativ:");
                Console.WriteLine("1. Visa kommande evenemang");
                Console.WriteLine("2. Köp biljett till ett evenemang");
                Console.WriteLine("3. Visa bokade biljetter");
                Console.WriteLine("4. Avboka biljett");
                Console.WriteLine("5. Avsluta programmet");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        Console.WriteLine("Kommande evenemang:");
                        foreach (var ev in events)
                        {
                            Console.WriteLine($"{ev.Name} - {ev.EventDate.ToShortDateString()}");
                            Console.WriteLine($"Biljetter släpps vid: {ev.TicketReleaseDate:yyyy-MM-dd HH:mm}");
                        }
                        break;

                    case "2":
                        Console.WriteLine("Vilken av dessa evenemang vill du köpa biljett för:");
                        for (int i = 0; i < events.Count; i++)
                        {
                            Console.WriteLine($"{i + 1}. {events[i].Name} - {events[i].EventDate.ToShortDateString()}");
                        }

                        if (int.TryParse(Console.ReadLine(), out int eventIndex) && eventIndex > 0 && eventIndex <= events.Count)
                        {
                            var selectedEvent = events[eventIndex - 1];

                            if (DateTime.Now < selectedEvent.TicketReleaseDate)
                            {
                                Console.WriteLine($"Biljetter för {selectedEvent.Name} släpps vid {selectedEvent.TicketReleaseDate:yyyy-MM-dd HH:mm}. Vänligen återkom senare.");
                                break;
                            }

                            Console.WriteLine("Hur många biljetter vill du köpa? (Max 5 personer):");
                            if (!int.TryParse(Console.ReadLine(), out int ticketCount) || ticketCount < 1 || ticketCount > 5)
                            {
                                Console.WriteLine("Ogiltigt antal biljetter. Ange ett tal mellan 1 och 5.");
                                break;
                            }

                            bool correctSeatSelection = false;
                            List<int> seatNumbers = new List<int>();

                            while (!correctSeatSelection)
                            {
                                Console.WriteLine("Välj plats att reservera:");
                                selectedEvent.ShowSeats();
                                Console.WriteLine("Ange platsnummer som du vill reservera, separera med komma (ex: 1,2,5):");

                                var input = Console.ReadLine();
                                seatNumbers = input.Split(',')
                                                   .Select(s => int.TryParse(s.Trim(), out int num) ? num : -1)
                                                   .Where(n => n > 0)
                                                   .ToList();

                                if (seatNumbers.Count == ticketCount)
                                {
                                    correctSeatSelection = true;
                                }
                                else
                                {
                                    Console.WriteLine($"Antalet valda platser matchar inte antalet biljetter. Vänligen välj exakt {ticketCount} platser.");
                                }
                            }

                            if (selectedEvent.ReserveSelectedSeats(seatNumbers, timeoutSeat =>
                            {
                                Console.WriteLine($"\nTiden för reservation av plats {timeoutSeat} har gått ut.");
                                selectedEvent.Seats.First(s => s.Number == timeoutSeat).Status = SeatStatus.Ledig;
                            }))
                            {
                                Console.WriteLine("Vill du slutföra köpet för de reserverade biljetterna? (j/n)");
                                string confirmation = Console.ReadLine();
                                if (confirmation.ToLower() == "j")
                                {
                                    selectedEvent.CompletePurchase(seatNumbers);
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Ogiltigt val, försök igen.");
                        }
                        break;

                    case "3":
                        Console.WriteLine("Visa bokade biljetter:");
                        foreach (var ev in events)
                        {
                            Console.WriteLine($"\n{ev.Name}:");
                            ev.Seats.Where(s => s.Status == SeatStatus.Upptagen)
                                     .ToList()
                                     .ForEach(s => Console.WriteLine($"Plats {s.Number} - {s.Type}"));
                        }
                        break;

                    case "4":
                        Console.WriteLine("Avboka biljett - ange evenemangsnummer:");
                        for (int i = 0; i < events.Count; i++)
                        {
                            Console.WriteLine($"{i + 1}. {events[i].Name}");
                        }

                        if (int.TryParse(Console.ReadLine(), out int cancelEventIndex) && cancelEventIndex > 0 && cancelEventIndex <= events.Count)
                        {
                            var cancelEvent = events[cancelEventIndex - 1];

                            Console.WriteLine("Ange platsnummer för att avboka:");
                            if (int.TryParse(Console.ReadLine(), out int cancelSeatNumber))
                            {
                                var seat = cancelEvent.Seats.FirstOrDefault(s => s.Number == cancelSeatNumber && s.Status == SeatStatus.Reserverad);
                                if (seat != null)
                                {
                                    seat.Status = SeatStatus.Ledig;
                                    Console.WriteLine($"Reservation för plats {cancelSeatNumber} har avbokats.");
                                }
                                else
                                {
                                    Console.WriteLine("Ingen reservation hittades för denna plats.");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Ogiltigt platsnummer. Ange ett giltigt heltal.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Ogiltigt val, försök igen.");
                        }
                        break;

                    case "5":
                        Console.WriteLine("Avslutar programmet. Tack för att du använde Biljettshopen!");
                        running = false;
                        break;

                    default:
                        Console.WriteLine("Ogiltigt val, försök igen.");



                        break;
                }
            }
        }
    }
}
