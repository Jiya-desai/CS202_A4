using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlarmClockForm
{
    public partial class Form1 : Form
    {
        // Define the delegate for the event
        public delegate void AlarmEventHandler(object source, EventArgs args);
        
        // Define the event using the delegate
        public event AlarmEventHandler raiseAlarm;
        
        // Random for generating colors
        private Random random = new Random();
        
        // Timer for updating the form
        private System.Windows.Forms.Timer colorTimer;
        
        // Flag to indicate if the alarm is running
        private bool isAlarmRunning = false;
        
        // Target time
        private TimeSpan targetTime;
        
        public Form1()
        {
            InitializeComponent();
            
            // Subscribe to the event
            raiseAlarm += Ring_alarm;
            
            // Initialize the timer
            colorTimer = new System.Windows.Forms.Timer();
            colorTimer.Interval = 1000; // 1 second
            colorTimer.Tick += ColorTimer_Tick;
        }
        
        // Method to handle the button click event
        private void btnStart_Click(object sender, EventArgs e)
        {
            if (isAlarmRunning)
            {
                MessageBox.Show("Alarm is already running.");
                return;
            }
            
            try
            {
                // Parse the time input
                string timeInput = txtTime.Text;
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
                targetTime = new TimeSpan(hours, minutes, seconds);
                
                // Start the alarm
                StartAlarm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        // Method to start the alarm
        private void StartAlarm()
        {
            isAlarmRunning = true;
            colorTimer.Start();
            
            // Start a task to check for the target time
            Task.Run(() => CheckAlarmTime());
        }
        
        // Method to check if the current time matches the target time
        private async void CheckAlarmTime()
        {
            while (isAlarmRunning)
            {
                // Get current time
                TimeSpan currentTime = DateTime.Now.TimeOfDay;
                
                // Update the current time display on the UI thread
                Invoke(new Action(() => 
                {
                    lblCurrentTime.Text = $"Current Time: {currentTime.Hours:D2}:{currentTime.Minutes:D2}:{currentTime.Seconds:D2}";
                    lblTargetTime.Text = $"Target Time: {targetTime.Hours:D2}:{targetTime.Minutes:D2}:{targetTime.Seconds:D2}";
                }));
                
                // Check if current time matches target time
                if (currentTime.Hours == targetTime.Hours && 
                    currentTime.Minutes == targetTime.Minutes && 
                    currentTime.Seconds == targetTime.Seconds)
                {
                    // Use Invoke to call the method on the UI thread
                    Invoke(new Action(() => OnRaiseAlarm()));
                    break;
                }
                
                // Wait for 100 milliseconds before checking again
                await Task.Delay(100);
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
        
        // Method to handle the timer tick event
        private void ColorTimer_Tick(object sender, EventArgs e)
        {
            // Change the background color
            this.BackColor = GetRandomColor();
        }
        
        // Method to get a random color
        private Color GetRandomColor()
        {
            return Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));
        }
        
        // Method that will be called when the event is raised
        public void Ring_alarm(object source, EventArgs args)
        {
            // Stop the timer
            colorTimer.Stop();
            isAlarmRunning = false;
            
            // Show the message
            MessageBox.Show("ALARM! The target time has been reached!", "Alarm", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}