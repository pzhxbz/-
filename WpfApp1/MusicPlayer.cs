using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WpfApp1
{
    class MusicPlayer
    {
        private MediaPlayer player;
        private bool isPlaying = false;
        private bool isOver = true;

        public MusicPlayer()
        {
            player = new MediaPlayer();
            player.MediaEnded += new EventHandler ((object e,EventArgs args)=>{
                isOver = true;
                player.Close();
                isPlaying = false;
            });
        }

        public void PlayMusic(string path)
        {
            if(isPlaying != false)
            {
                return;
            }
            isPlaying = true;
            Action action1 = () =>
            {
                player.Open(new Uri(path, UriKind.Relative));
                player.Play();
            };
            player.Dispatcher.BeginInvoke(action1);
            isOver = false;
            
        }
        public void Stop()
        {
            if(isPlaying == false)
            {
                return;
            }
            Action action1 = () =>
            {
                player.Stop();
                player.Close();
            };
            player.Dispatcher.BeginInvoke(action1);
            isPlaying = false;
            isOver = true;
        }

        public bool IsOver()
        {
            return this.isOver;
        }
    }
}
