using Code.Client.Managers;

namespace Code.Client.GameStates
{
    public abstract class GameStateBase
    {
        protected readonly GameManager GameManager;
        
        protected GameStateBase(GameManager gameManager)
        {
            GameManager = gameManager;
        }
        
        public virtual void OnEnter(object context = null) {}
        public virtual void OnExit() {}
        public virtual void Update() {}
        public virtual void FixedUpdate() {}
    }
}