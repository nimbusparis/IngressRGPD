public struct LatLng
{
    public double Lat { get; }
    public double Lng { get; }

    public LatLng(double lat, double lng)
    {
        Lat = lat;
        Lng = lng;
    }

    public bool Equals(LatLng other)
    {
        return Lat.Equals(other.Lat) && Lng.Equals(other.Lng);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        return obj is LatLng && Equals((LatLng)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Lat.GetHashCode() * 397) ^ Lng.GetHashCode();
        }
    }
}