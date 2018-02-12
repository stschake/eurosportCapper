namespace esnew.model
{
    public enum Country
    {
        UnitedKingdom,
        Germany
    }

    public static class CountryExtensions
    {
        public static Location ToLocation(this Country country)
        {
            switch (country)
            {
                case Country.UnitedKingdom: // This is London
                    return new Location { Latitude = 51.508530, Longitude = -0.076132 };
                case Country.Germany: // This is Berlin
                    return new Location { Latitude = 52.520008, Longitude = 13.404954 };
            }

            return null;
        }
    }

}