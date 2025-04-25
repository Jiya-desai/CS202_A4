using System;
using System.Threading;

namespace AlarmClockConsole
{
    // Publisher class that will raise the event
    public class AlarmClock
    {
        // Define the delegate for the event
        public delegate void AlarmEventHandler(object source, EventArgs args);
        
        // Define the event using the delegate
        public event AlarmEventHandler raiseAlarm;
        
        // Method to start the alarm clock
        public void Start(TimeSpan targetTime)
        {
            Console.WriteLine("Alarm clock started. Waiting for the target time...");
            
            // Keep checking until the target time is reached
            while (true)
            {
                // Get current time
                TimeSpan currentTime = DateTime.Now.TimeOfDay;
                
                // Format and display current time
                Console.WriteLine($"Current time: {currentTime.Hours:D2}:{currentTime.Minutes:D2}:{currentTime.Seconds:D2}");
                
                // Check if current time matches target time
                if (currentTime.Hours == targetTime.Hours && 
                    currentTime.Minutes == targetTime.Minutes && 
                    currentTime.Seconds == targetTime.Seconds)
                {
                    // Raise the event
                    OnRaiseAlarm();
                    break;
                }
                
                // Wait for 1 second before checking again
                Thread.Sleep(1000);
            }
        }
        
        // Method to raise the event
        protected virtual void OnRaiseAlarm()
        {
            if (raiseAlarm != null)
            {
                raiseAlarm(this, EventArgs.Empty);
            }
        }
    }
    
    // Subscriber class
    public class AlarmListener
    {
        // Method that will be called when the event is raised
        public void Ring_alarm(object source, EventArgs args)
        {
            Console.WriteLine("ALARM! The target time has been reached!");
            Console.WriteLine("Ring! Ring! Ring!");
        }
    }
    
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Alarm Clock Application");
            Console.WriteLine("======================");
            
            // Get target time from user
            Console.Write("Enter target time in HH:MM:SS format: ");
            string timeInput = Console.ReadLine();
            
            try
            {
                // Parse the time input
                string[] timeParts = timeInput.Split(':');
                if (timeParts.Length != 3)
                {
                    throw new FormatException("Invalid time format. Please use HH:MM:SS format.");
                }
                
                int hours = int.Parse(timeParts[0]);
                int minutes = int.Parse(timeParts[1]);
                int seconds = int.Parse(timeParts[2]);
                
                // Validate time components
                if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59 || seconds < 0 || seconds > 59)
                {
                    throw new ArgumentOutOfRangeException("Invalid time values. Hours should be 0-23, minutes and seconds should be 0-59.");
                }
                
                // Create target TimeSpan
                TimeSpan targetTime = new TimeSpan(hours, minutes, seconds);
                
                // Create publisher and subscriber
                AlarmClock alarmClock = new AlarmClock();
                AlarmListener listener = new AlarmListener();
                
                // Subscribe to the event
                alarmClock.raiseAlarm += listener.Ring_alarm;
                
                // Start the alarm clock
                alarmClock.Start(targetTime);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}