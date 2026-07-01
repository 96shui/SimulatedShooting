namespace BaseUtil
{
    /// <summary>
    /// 管理器基类。继承 <typeparamref name="T"/> 的子类自动获得单例访问能力。
    /// </summary>
    public abstract class BaseManager<T> where T : BaseManager<T>, new()
    {
        static T instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new T();
                    instance.Init();
                }

                return instance;
            }
        }

        public static bool HasInstance => instance != null;

        protected virtual void Init()
        {
        }

        protected virtual void OnDestroy()
        {
        }

        public static void DestroyInstance()
        {
            if (instance == null)
            {
                return;
            }

            instance.OnDestroy();
            instance = null;
        }
    }
}
