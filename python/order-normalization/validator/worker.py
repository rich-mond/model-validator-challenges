import importlib.util
import json
import sys

spec = importlib.util.spec_from_file_location("order_normalizer", "/candidate/src/order_normalizer.py")
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
event = json.load(sys.stdin)
result = module.normalize_order(event)
json.dump({"result": result, "event": event}, sys.stdout)
