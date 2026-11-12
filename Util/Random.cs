/*
 * Nicole Swierstra
 * Singleton Random
 *
 * likely useless
 */

class Rand {
    private static Random instance;

    private static Random Instance() {
        if (instance == null) 
            instance = new Random((int)DateTime.Now.Ticks);
        return instance;
    }

    public static int GetInt(int upper_bound = int.MaxValue, int lower_bound = 0) {
        return Instance().Next(lower_bound, upper_bound);
    }

    public static float GetFloat() {
        return Instance().NextSingle();
    }

    public static void Shuffle<T>(Span<T> arr) {
        Instance().Shuffle(arr);
    }
}