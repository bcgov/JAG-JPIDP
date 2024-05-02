<#import "template.ftl" as layout>
<@layout.registrationLayout displayMessage=true displayInfo=false; section>
<#if section = "header">
  <h1>BC Corrections Service Login</h1>
  <h2>Please enter your Corrections Service (CS) Number</h2><#elseif section = "form">
<div id="kc-form">
    <div id="kc-form-wrapper"><#if realm.password>
        <form id="kc-form-login" onsubmit="login.disabled = true; return true;" action="${url.loginAction}" method="post">
            <input class="digit" id="cs_num_1" type="number" min="0" max="9" maxlength="1" autofocus></input>
            <input class="digit" id="cs_num_2" type="number" min="0" max="9" maxlength="1"></input>
            <input class="digit" id="cs_num_3" type="number" min="0" max="9" maxlength="1"></input>
            <input class="digit" id="cs_num_4" type="number" min="0" max="9" maxlength="1"></input>
            <input class="digit" id="cs_num_5" type="number" min="0" max="9" maxlength="1"></input>
            <input class="digit" id="cs_num_6" type="number" min="0" max="9" maxlength="1"></input>
            <input class="digit" id="cs_num_7" type="number" min="0" max="9" maxlength="1"></input>
            <input class="digit" id="cs_num_8" type="number" min="0" max="9" maxlength="1"></input>
            <div id="kc-form-buttons" class="${properties.kcFormGroupClass!}">
                <input type="hidden" id="id-hidden-input" name="credentialId" <#if auth.selectedCredential?has_content>value="${auth.selectedCredential}"
                </#if>/>
                <input tabindex="7" class="${properties.kcButtonClass!} ${properties.kcButtonPrimaryClass!} ${properties.kcButtonBlockClass!} ${properties.kcButtonLargeClass!}" name="login" id="kc-login" type="submit" value="${msg("doLogIn")}"/>
            </div>
        </form>
    </#if><#if section = "socialProviders"><#if social.providers??>
    <h1>Prv</h1><#else>
    <h2>No Providers</h2>
</#if></#if></#if></@layout.registrationLayout>