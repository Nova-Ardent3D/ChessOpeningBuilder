using UnityEngine;

namespace Board.Audio
{
    public class MoveAudio : MonoBehaviour
    {
        public enum Clips
        {
            Move,
            Capture,
            Castle,
            Check,
            Promotion,
        }

        [SerializeField] AudioSource Move;
        [SerializeField] AudioSource Capture;
        [SerializeField] AudioSource Castle;
        [SerializeField] AudioSource Check;
        [SerializeField] AudioSource Promotion;

        public void Play(Clips clips)
        {
            switch (clips)
            {
                case Clips.Move: 
                    Move.Play();
                    break;
                case Clips.Capture:
                    Capture.Play();
                    break;
                case Clips.Castle:
                    Castle.Play();
                    break;
                case Clips.Check:
                    Check.Play();
                    break;
                case Clips.Promotion:
                    Promotion.Play();
                    break;
            }
        }
    }
}
