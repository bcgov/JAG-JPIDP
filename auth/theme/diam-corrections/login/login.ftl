<#import "template.ftl" as layout>
<@layout.registrationLayout displayMessage=true displayInfo=false; section>
   <#if section = "header">
     <h1>BC Corrections Service Login</h1>
     <h2>Please enter your Client Services (CS) Number</h2>
     <input type="number"></input>
   </#if>
</@layout.registrationLayout>
