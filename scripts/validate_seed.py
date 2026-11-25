import json
from pathlib import Path

root = Path(__file__).resolve().parents[1] / "Data"
expected = {"customers":50,"contracts":80,"invoices":300,"tariffs":15,"services":20,"customer-services":100,"employees":20,"troubleshooting":40,"outages":10,"tickets":30}
data = {k: json.loads((root / f"{k}.json").read_text(encoding="utf-8")) for k in expected}
for k, count in expected.items():
    assert len(data[k]) == count, k
    assert len({r["id"] for r in data[k]}) == count, k
customers = {r["id"] for r in data["customers"]}
contracts = {r["id"]: r for r in data["contracts"]}
tariffs = {r["id"] for r in data["tariffs"]}
services = {r["id"] for r in data["services"]}
employees = {r["id"] for r in data["employees"]}
assert all(c["customerId"] in customers and c["tariffId"] in tariffs for c in contracts.values())
assert all(contracts[i["contractId"]]["customerId"] == i["customerId"] for i in data["invoices"])
assert all(contracts[s["contractId"]]["customerId"] == s["customerId"] and s["serviceId"] in services for s in data["customer-services"])
assert all(t["customerId"] in customers and t["employeeId"] in employees for t in data["tickets"])
assert all(c["email"].endswith("@example.invalid") for c in data["customers"])
assert all(e["email"].endswith("@example.invalid") for e in data["employees"])
print("All seed checks passed:", expected)
