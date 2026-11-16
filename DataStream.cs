/**
 * Broke these out into their own classes. They could be template classes tbh
 */
using NumSharp;

abstract class DataStream {
    protected DBCObj.SignalInfo siginfo;
    protected List<ulong> timestamp;    

    public int getLength() => timestamp.Count;
    public List<ulong> getTimestamps() => timestamp;

    protected DataStream(DBCObj.SignalInfo siginfo) {
        timestamp = new List<ulong>();
        this.siginfo = siginfo;
    }

    /* returns the next index that is lower than the timestamp */
    protected int getinsertionidx(ulong newtimestamp) {
        int idx = timestamp.Count + 1;
        
        do { 
            idx--; 
            if (idx == 0) return 0; 
        } while(timestamp[idx - 1] > newtimestamp);
        
        return idx;
    }

    public abstract void append(ulong timestamp, object data);

    public abstract string strvalueof(int idx);
    public abstract double valueof(int idx);
    public abstract object rawval(int idx);
    
    public abstract Array resampleraw(ulong[] newtimestamps);
    
    protected Array resampledouble(ulong[] newtimestamps) {
        NDArray olddata = np.array(new double[timestamp.Count]);
        for (int i = 0; i < timestamp.Count; i++) {
            olddata[i] = valueof(i);
        }

        /* could we replace this with a hard coded implementation? yeah probably. hopefully this is faster tho */
        NDArray retval = np.interp(newtimestamps, timestamp.ToArray<ulong>(), olddata);
        return retval.ToArray<double>();
    }
}

class FloatStream: DataStream {
    public List<float> data;
    public FloatStream(DBCObj.SignalInfo siginfo) : base(siginfo)
    {
        data = new List<float>();
    }

    public override void append(ulong timestamp, object data)
    {
        int newidx = getinsertionidx(timestamp);
        this.timestamp.Insert(newidx, timestamp);
        this.data.Insert(newidx, (float)data);
    }

    public override object rawval(int idx) => data[idx];
    public override double valueof(int idx) => data[idx] * siginfo.scale;
    public override string strvalueof(int idx) => (data[idx] * siginfo.scale).ToString();

    public override Array resampleraw(ulong[] newtimestamps)
    {
        return resampledouble(newtimestamps);
    }
}

class DoubleStream: DataStream {
    public List<double> data;
    public DoubleStream(DBCObj.SignalInfo siginfo) : base(siginfo)
    {
        data = new List<double>();
    }

    public override void append(ulong timestamp, object data)
    {
        int newidx = getinsertionidx(timestamp);
        this.timestamp.Insert(newidx, timestamp);
        this.data.Insert(newidx, (double)data);
    }

    public override object rawval(int idx) => data[idx];
    public override double valueof(int idx) => data[idx] * siginfo.scale;
    public override string strvalueof(int idx) => (data[idx] * siginfo.scale).ToString();

    public override Array resampleraw(ulong[] newtimestamps)
    {
        return resampledouble(newtimestamps);
    }
}

class SignedStream: DataStream {
    public List<long> data;
    public SignedStream(DBCObj.SignalInfo siginfo) : base(siginfo)
    {
        data = new List<long>();
    }

    public override void append(ulong timestamp, object data)
    {
        int newidx = getinsertionidx(timestamp);
        this.timestamp.Insert(newidx, timestamp);
        this.data.Insert(newidx, (long)data);
    }

    public override object rawval(int idx) => data[idx];
    public override double valueof(int idx) => data[idx] * siginfo.scale;
    public override string strvalueof(int idx) => (data[idx] * siginfo.scale).ToString();

    public override Array resampleraw(ulong[] newtimestamps)
    {
        return resampledouble(newtimestamps);
    }
}

class UnsignedStream: DataStream {
    public List<ulong> data;
    public UnsignedStream(DBCObj.SignalInfo siginfo) : base(siginfo)
    {
        data = new List<ulong>();
    }
    
    public override void append(ulong timestamp, object data)
    {
        int newidx = getinsertionidx(timestamp);
        this.timestamp.Insert(newidx, timestamp);
        this.data.Insert(newidx, (ulong)data);
    }

    public override object rawval(int idx) => data[idx];
    public override double valueof(int idx) => data[idx] * siginfo.scale;
    /* allows for bitfields */
    public override string strvalueof(int idx) => (siginfo.scale == 1.0) ? data[idx].ToString() : (data[idx] * siginfo.scale).ToString();

    public override Array resampleraw(ulong[] newtimestamps)
    {
        /* don't interpolate if the data is unscaled */
        if (siginfo.scale == 1)
        {
            ulong[] newdata = new ulong[newtimestamps.Length];
            int high_timestamp_idx = 0;
            for (int i = 0; i < newtimestamps.Length; i++) {
                while (timestamp[high_timestamp_idx] < newtimestamps[i]){
                    if (high_timestamp_idx >= timestamp.Count) {
                        break;
                    }
                    high_timestamp_idx++;
                }
                int low_timestamp_idx = Math.Max(0, high_timestamp_idx - 1);
                newdata[i] = data[low_timestamp_idx];
            }
            return newdata;
        }
        else
        {
            return resampledouble(newtimestamps);
        }
    }
}