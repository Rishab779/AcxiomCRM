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
    return login_resp.read().decode('utf-8')

def test_security_hardening():
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

    print("=== TESTING COMPREHENSIVE CRM SECURITY HARDENING ===")

    # 1. Login as Sales Executive 1 (Alex Miller: sales@acxiomcrm.local)
    sales1_op, _ = get_session()
    login(sales1_op, "sales@acxiomcrm.local", "Sales@12345")

    # 2. Verify that Sales 1 on /FollowUp/Create only sees their assigned records in dropdowns
    f_create_html = sales1_op.open(f"{BASE_URL}/FollowUp/Create").read().decode('utf-8')
    # Customer #3 (BioHealth Diagnostics) belongs to Jordan Lee (sales2), should NOT be in Sales 1 dropdown!
    check("Cross-rep Customer #3 omitted from FollowUp dropdown", "BioHealth Diagnostics" not in f_create_html)
    # Customer #1 (Nexus Global) belongs to Sales 1, SHOULD be in dropdown
    check("Owned Customer #1 present in FollowUp dropdown", "Nexus Global" in f_create_html)

    # 3. Test Service-level protection: Sales 1 attempts to POST an Opportunity linked to Customer #3 (owned by Sales 2)
    token = get_csrf(sales1_op.open(f"{BASE_URL}/Opportunity/Create").read().decode('utf-8'))
    data = urllib.parse.urlencode({
        "OpportunityName": "Illegitimate Opp",
        "CustomerId": 3, # Belongs to sales2
        "Amount": "50000",
        "Stage": "Proposal",
        "Probability": "50",
        "ExpectedCloseDate": "2026-12-31",
        "__RequestVerificationToken": token
    }).encode('utf-8')
    req = urllib.request.Request(f"{BASE_URL}/Opportunity/Create", data=data)
    opp_resp = sales1_op.open(req).read().decode('utf-8')
    check("Reject Cross-Rep Opportunity Association", "You are not authorized to create an opportunity for this customer" in opp_resp)

    # 4. Test Service-level protection: Sales 1 attempts to POST a FollowUp linked to Customer #3 (owned by Sales 2)
    token = get_csrf(sales1_op.open(f"{BASE_URL}/FollowUp/Create").read().decode('utf-8'))
    data = urllib.parse.urlencode({
        "CustomerId": 3, # Belongs to sales2
        "FollowUpDate": "2026-12-31T10:00",
        "FollowUpType": "Call",
        "Remarks": "Illegal follow-up",
        "__RequestVerificationToken": token
    }).encode('utf-8')
    req = urllib.request.Request(f"{BASE_URL}/FollowUp/Create", data=data)
    f_resp = sales1_op.open(req).read().decode('utf-8')
    check("Reject Cross-Rep FollowUp Association", "You are not authorized to associate this follow-up with the specified customer" in f_resp)

    # 5. Test Service-level protection: Sales 1 attempts to POST an Activity linked to Customer #3 (owned by Sales 2)
    token = get_csrf(sales1_op.open(f"{BASE_URL}/Activity/Create").read().decode('utf-8'))
    data = urllib.parse.urlencode({
        "CustomerId": 3, # Belongs to sales2
        "ActivityType": "Call",
        "Subject": "Illegal Call",
        "ActivityDate": "2026-12-31T10:00",
        "__RequestVerificationToken": token
    }).encode('utf-8')
    req = urllib.request.Request(f"{BASE_URL}/Activity/Create", data=data)
    act_resp = sales1_op.open(req).read().decode('utf-8')
    check("Reject Cross-Rep Activity Association", "You are not authorized to associate this activity with the specified customer" in act_resp)

    # 6. Test User Filtering scope: Sales 1 index page should not show user dropdown with other staff
    lead_idx_html = sales1_op.open(f"{BASE_URL}/Lead").read().decode('utf-8')
    # Sales 1 should only see themselves in the user list, not manager or admin
    check("Manager omitted from Sales 1 filter dropdown", "manager@acxiomcrm.local" not in lead_idx_html)

    # 7. Login as Manager (Sarah Connor: manager@acxiomcrm.local)
    mgr_op, _ = get_session()
    login(mgr_op, "manager@acxiomcrm.local", "Manager@12345")

    # Manager sees their team members (Alex Miller & Jordan Lee) in user dropdown
    mgr_lead_html = mgr_op.open(f"{BASE_URL}/Lead").read().decode('utf-8')
    check("Team member present in Manager filter dropdown", "Alex Miller" in mgr_lead_html)

    # Manager on /Audit only sees logs of their team, not Admin system init
    mgr_audit_html = mgr_op.open(f"{BASE_URL}/Audit").read().decode('utf-8')
    check("Admin security events omitted from Manager Audit", "SystemInitialize" not in mgr_audit_html)

    print(f"\nRESULTS: {passed}/{total} tests passed.")
    if passed == total:
        print("ALL SECURITY HARDENING CHECKS PASSED PERFECTLY!")
    else:
        print("SOME CHECKS FAILED.")

if __name__ == "__main__":
    test_security_hardening()
