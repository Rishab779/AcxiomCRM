import urllib.request
import urllib.parse
import http.cookiejar
import json
import re
import sys

BASE_URL = "http://127.0.0.1:5123"

def get_session():
    cj = http.cookiejar.CookieJar()
    opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cj))
    return opener, cj

def get_csrf(html):
    match = re.search(r'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"', html)
    if not match:
        match = re.search(r'value="([^"]+)"\s+name="__RequestVerificationToken"', html)
    return match.group(1) if match else None

def login(opener, email, password):
    resp = opener.open(f"{BASE_URL}/Account/Login")
    html = resp.read().decode('utf-8')
    token = get_csrf(html)
    if not token:
        raise Exception(f"Failed to find CSRF token on login page for {email}")
    data = urllib.parse.urlencode({
        "Email": email,
        "Password": password,
        "RememberMe": "false",
        "__RequestVerificationToken": token
    }).encode('utf-8')
    req = urllib.request.Request(f"{BASE_URL}/Account/Login", data=data)
    login_resp = opener.open(req)
    res_html = login_resp.read().decode('utf-8')
    return res_html

def test_suite():
    passed = 0
    total = 0
    def check(name, condition, msg=""):
        nonlocal passed, total
        total += 1
        if condition:
            print(f"[PASS] {name}")
            passed += 1
        else:
            print(f"[FAIL] {name}: {msg}")

    print("=== STARTING ACXIOMCRM VERIFICATION ===")

    # 1. Admin Login
    admin_op, _ = get_session()
    admin_home = login(admin_op, "admin@acxiomcrm.local", "Admin@12345")
    check("Admin Login Successful", "Dashboard" in admin_home or "AcxiomCRM" in admin_home)

    # 2. Check Dashboard
    dash_html = admin_op.open(f"{BASE_URL}/Dashboard").read().decode('utf-8')
    check("Dashboard Loads", "Total Customers" in dash_html and "Opportunity Pipeline" in dash_html)
    check("Chart.js present on Dashboard", "chart.js" in dash_html.lower() or "new Chart" in dash_html)

    # 3. Check All Reports
    reports = [
        "CustomerReport",
        "LeadReport",
        "OpportunityReport",
        "PipelineReport",
        "FollowUpReport",
        "ConversionReport",
        "UserActivityReport",
        "AuditReport"
    ]
    for rep in reports:
        try:
            r_html = admin_op.open(f"{BASE_URL}/Report/{rep}").read().decode('utf-8')
            check(f"Report: {rep}", len(r_html) > 0 and "error" not in r_html.lower()[:300])
        except Exception as e:
            check(f"Report: {rep}", False, str(e))

    # 4. Check Audit Logs view
    try:
        audit_html = admin_op.open(f"{BASE_URL}/Audit").read().decode('utf-8')
        check("Audit Log Page Loads", "Audit Log" in audit_html and "<table" in audit_html)
    except Exception as e:
        check("Audit Log Page Loads", False, str(e))

    # 5. Check User Management view
    try:
        user_html = admin_op.open(f"{BASE_URL}/User").read().decode('utf-8')
        check("User Management Page Loads", "User Management" in user_html and "admin@acxiomcrm.local" in user_html)
    except Exception as e:
        check("User Management Page Loads", False, str(e))

    # 6. Test Sales Executive Login & Scope Restrictions
    sales_op, _ = get_session()
    sales_home = login(sales_op, "sales@acxiomcrm.local", "Sales@12345")
    check("Sales Exec Login", "AcxiomCRM" in sales_home)

    # 7. Security Check: Sales Exec forbidden from User Management and Audit Logs
    try:
        user_attempt = sales_op.open(f"{BASE_URL}/User").read().decode('utf-8')
        check("Sales Exec Forbidden from /User", "AccessDenied" in user_attempt or "Access Denied" in user_attempt)
    except urllib.error.HTTPError as e:
        check("Sales Exec Forbidden from /User", e.code in [401, 403])

    try:
        audit_attempt = sales_op.open(f"{BASE_URL}/Audit").read().decode('utf-8')
        check("Sales Exec Forbidden from /Audit", "AccessDenied" in audit_attempt or "Access Denied" in audit_attempt)
    except urllib.error.HTTPError as e:
        check("Sales Exec Forbidden from /Audit", e.code in [401, 403])

    # 8. Check REST APIs with Admin session
    try:
        cust_api = admin_op.open(f"{BASE_URL}/api/customers").read().decode('utf-8')
        cust_list = json.loads(cust_api)
        check("REST API GET /api/customers", isinstance(cust_list, list) and len(cust_list) > 0)
    except Exception as e:
        check("REST API GET /api/customers", False, str(e))

    try:
        lead_api = admin_op.open(f"{BASE_URL}/api/leads").read().decode('utf-8')
        lead_list = json.loads(lead_api)
        check("REST API GET /api/leads", isinstance(lead_list, list) and len(lead_list) > 0)
    except Exception as e:
        check("REST API GET /api/leads", False, str(e))

    try:
        opp_api = admin_op.open(f"{BASE_URL}/api/opportunities").read().decode('utf-8')
        opp_list = json.loads(opp_api)
        check("REST API GET /api/opportunities", isinstance(opp_list, list) and len(opp_list) > 0)
    except Exception as e:
        check("REST API GET /api/opportunities", False, str(e))

    # 9. Test REST API POST Customer
    import random
    rand_id = random.randint(1000, 9999)
    new_cust_payload = json.dumps({
        "customerName": f"Automated Corp {rand_id}",
        "email": f"auto{rand_id}@example.com",
        "phone": f"+1-555-{rand_id}",
        "companyName": f"Automated Enterprise {rand_id}",
        "city": "Austin",
        "state": "TX"
    }).encode('utf-8')
    try:
        req = urllib.request.Request(f"{BASE_URL}/api/customers", data=new_cust_payload, headers={"Content-Type": "application/json"})
        create_resp = admin_op.open(req)
        cust_res = json.loads(create_resp.read().decode('utf-8'))
        new_cust_id = cust_res.get("customerId")
        check("REST API POST /api/customers", new_cust_id is not None)

        # 10. Test REST API PUT Customer
        put_payload = json.dumps({
            "customerName": f"Automated Corp Updated {rand_id}",
            "email": f"auto{rand_id}@example.com",
            "phone": f"+1-555-{rand_id}",
            "companyName": f"Automated Enterprise {rand_id}",
            "city": "Dallas",
            "state": "TX",
            "status": "Active"
        }).encode('utf-8')
        req = urllib.request.Request(f"{BASE_URL}/api/customers/{new_cust_id}", data=put_payload, headers={"Content-Type": "application/json"}, method="PUT")
        put_resp = admin_op.open(req)
        check("REST API PUT /api/customers/{id}", put_resp.status in [200, 204])

        # 11. Test REST API DELETE Customer
        req = urllib.request.Request(f"{BASE_URL}/api/customers/{new_cust_id}", headers={"Content-Type": "application/json"}, method="DELETE")
        del_resp = admin_op.open(req)
        check("REST API DELETE /api/customers/{id}", del_resp.status in [200, 204])
    except Exception as e:
        check("REST API CRUD Customer", False, str(e))

    # 12. Check Swagger UI
    try:
        sw_html = admin_op.open(f"{BASE_URL}/swagger/index.html").read().decode('utf-8')
        check("Swagger UI Accessible", "Swagger UI" in sw_html or "swagger" in sw_html.lower())
    except Exception as e:
        check("Swagger UI Accessible", False, str(e))

    # 13. Test Business Rules Validation: Negative Opportunity Amount
    try:
        invalid_opp_payload = json.dumps({
            "opportunityName": "Invalid Opp",
            "customerId": 1,
            "amount": -500,
            "stage": "Qualification",
            "probability": 50,
            "expectedCloseDate": "2026-12-31"
        }).encode('utf-8')
        req = urllib.request.Request(f"{BASE_URL}/api/opportunities", data=invalid_opp_payload, headers={"Content-Type": "application/json"})
        admin_op.open(req)
        check("Reject Negative Opportunity Amount", False, "Expected 400 Bad Request")
    except urllib.error.HTTPError as e:
        check("Reject Negative Opportunity Amount", e.code == 400, f"Got HTTP {e.code}")

    # 14. Test Business Rules Validation: Duplicate Customer Email
    try:
        dup_payload = json.dumps({
            "customerName": "Duplicate Tester",
            "email": "sarah.jenkins@nexusglobal.com", # existing seed customer
            "phone": "+1-888-000-1111",
            "companyName": "Duplicate Corp"
        }).encode('utf-8')
        req = urllib.request.Request(f"{BASE_URL}/api/customers", data=dup_payload, headers={"Content-Type": "application/json"})
        admin_op.open(req)
        check("Prevent Duplicate Customer Email", False, "Expected 400 or 409 Conflict")
    except urllib.error.HTTPError as e:
        check("Prevent Duplicate Customer Email", e.code in [400, 409], f"Got HTTP {e.code}")

    print(f"\nRESULTS: {passed}/{total} tests passed.")
    if passed == total:
        print("ALL VERIFICATION CHECKS PASSED PERFECTLY!")
    else:
        print("SOME CHECKS FAILED.")

if __name__ == "__main__":
    test_suite()
