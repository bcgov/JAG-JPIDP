<#import "template.ftl" as layout>
<@layout.registrationLayout displayMessage=true displayInfo=false; section>
   <#if section = "header">
     <h1>BC Corrections Service Login</h1>
     <h2>Please enter your Client Services (CS) Number</h2>
    <form action="${url.applicationsUrl}" method="post">
        <input type="hidden" id="stateChecker" name="stateChecker" value="${stateChecker}">
        <input type="hidden" id="referrer" name="referrer" value="${stateChecker}">

        </form>
   </#if>
</@layout.registrationLayout>
