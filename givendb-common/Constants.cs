namespace GivenDb.Common
{
    public static class Constants
    {
        public const string ExchangeMaster = "givendb";

        public static class RoutingKey
        {
            public const string NewSession = "new_session";
        }

        public static class Session
        {
            public static class RoutingKey
            {
                public const string NewDatabase = "new_database";
            }
        }

        public static class Queues
        {
            public const string Sessions = "givendb/sessions";            
        }
    }
}
