import os
import io
import serpent
from test_unicode import teststrings


files = [f for f in os.listdir(".") if f.startswith("data_") and f.endswith(".serpent")]

for f in files:
    print("Checking data file", f)
    resultstrings=[]
    with io.open(f, "rb") as inf:
        data = inf.read()
        data = data.split(b"\n\n")[:-1]
        # data = data[:-2] # XXX
        for d in data:
            try:
                resultstrings.append(serpent.loads(d))
            except Exception as x:
                print("SERPENT ERROR", type(x))

    if resultstrings==teststrings:
        print("OK")
    else:
        print("!!!FAIL!!!")
