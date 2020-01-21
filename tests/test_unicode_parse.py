import os
import io
import re
import serpent
from test_unicode import teststrings


files = [f for f in os.listdir(".") if f.startswith("data_") and f.endswith(".serpent")]

for f in files:
    print("Checking data file", f)
    resultstrings=[]
    with io.open(f, "rb") as inf:
        data = inf.read()
        data = re.split(b"~\n~\n", data)[:-1]
        assert len(data) == len(teststrings)
        # data = data[:-2] # XXX
        for num, d in enumerate(data, start=1):
            try:
                print("data item ",num,"...")
                resultstrings.append(serpent.loads(d))
            except Exception as x:
                print("\nSERPENT ERROR", type(x))

    if resultstrings==teststrings:
        print("OK")
    else:
        print("!!!FAIL!!!")
