using System;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Security
{
    public abstract class AccessProvider
    {
		protected readonly IUser StartupUser = new StartupUser();

        private static readonly object _lock = new object();
        private static AccessProvider _current;
        public static AccessProvider Current
        {
            get
            {
                if(_current == null)
                {
                    lock(_lock)
                    {
                        if(_current == null)
                        {
							try
							{
								var provider = (AccessProvider)TypeHandler.CreateInstance(RepositoryConfiguration.AccessProviderClassName);
                                provider.Initialize();
                                _current = provider;
							}
							catch (TypeNotFoundException) //rethrow
							{
								throw new ConfigurationException(String.Concat(SR.Exceptions.Configuration.Msg_AccessProviderImplementationDoesNotExist, ": ", RepositoryConfiguration.AccessProviderClassName));
							}
							catch (InvalidCastException) //rethrow
							{
								throw new ConfigurationException(String.Concat(SR.Exceptions.Configuration.Msg_InvalidAccessProviderImplementation, ": ", RepositoryConfiguration.AccessProviderClassName));
							}
                            Logger.WriteInformation("AccessProvider created.", Logger.GetDefaultProperties, _current);
                        }
                    }
                }
                return _current;
            }
        }

        protected virtual void Initialize()
        {
            // do nothing
        }

        public static bool IsInitialized { get { return _current != null; } }

        public abstract IUser GetCurrentUser();

        public void SetCurrentUser(IUser user)
        {
            if (user.Id == RepositoryConfiguration.SomebodyUserId)
                throw new SenseNetSecurityException("Cannot log in as 'Somebody' user.");
            DoSetCurrentUser(user);
        }
        protected abstract void DoSetCurrentUser(IUser user);

        public abstract bool IsAuthenticated { get; }

        private static SystemUser GetCurrentUserAsSystem()
        {
            return AccessProvider.Current.GetCurrentUser() as SystemUser;
        }

        public static void ChangeToSystemAccount()
        {
            // if the current user is the SYSTEM already, do nothing
            var sysuser = GetCurrentUserAsSystem();
            if (sysuser != null)
            {
                sysuser.Increment();
                return;
            }
            AccessProvider.Current.SetCurrentUser(new SystemUser(AccessProvider.Current.GetCurrentUser()));
        }
        public static void RestoreOriginalUser()
        {
            var sysuser = GetCurrentUserAsSystem();
            if (sysuser == null)
                return;
            if (sysuser.Decrement())
                return;
            AccessProvider.Current.SetCurrentUser(sysuser.OriginalUser);
        }

        public IUser GetOriginalUser()
        {
            var user = GetCurrentUser();
            var sysuser = user as SystemUser;
            if (sysuser == null)
                return user;
            return sysuser.OriginalUser;
        }
    }
}