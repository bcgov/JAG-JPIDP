<#import "template.ftl" as layout>
<@layout.registrationLayout; section>
<#--    <link src="${url.resourcesPath}/../diam-corrections/css/styles-20221128.css" rel="stylesheet" defer> </link>-->
<#--    <link src="${url.resourcesPath}/../diam-corrections/login/resources/css/styles-20221128.css" rel="stylesheet" ></link>-->
    <#if section = "header">
      <h1>BC Corrections Service Login</h1>
      <h2>Please enter your Corrections Service (CS) Number</h2><#elseif section = "form">
        <div id="kc-form">
            <div id="kc-form-wrapper">
            	<h3>
            		<span>${msg("csNumber.title")}</span>
            	</h3>
                <form class="${properties.kcFormClass!}" id="kc-form-login" action="${url.loginAction}" method="post">
    
                    <div class="${properties.kcFormGroupClass!}">
                        <input class="${properties.kcInputClass!}" id="cs_num_1" type="number" name="cs_num_1" min="0" max="9" maxlength="1" autofocus/>
                        <input class="${properties.kcInputClass!}" id="cs_num_2" type="number" name="cs_num_2" min="0" max="9" maxlength="1" />
                        <input class="${properties.kcInputClass!}" id="cs_num_3" type="number" name="cs_num_3" min="0" max="9" maxlength="1" />
                        <input class="${properties.kcInputClass!}" id="cs_num_4" type="number" name="cs_num_4" min="0" max="9" maxlength="1" />
                        <input class="${properties.kcInputClass!}" id="cs_num_5" type="number" name="cs_num_5" min="0" max="9" maxlength="1" />
                        <input class="${properties.kcInputClass!}" id="cs_num_6" type="number" name="cs_num_6" min="0" max="9" maxlength="1" />
                        <input class="${properties.kcInputClass!}" id="cs_num_7" type="number" name="cs_num_7" min="0" max="9" maxlength="1" />
                        <input class="${properties.kcInputClass!}" id="cs_num_8" type="number" name="cs_num_8" min="0" max="9" maxlength="1" />
                    </div>
                    <div id="kc-form-buttons" class="${properties.kcFormGroupClass!}">
                        <input tabindex="7" class="${properties.kcButtonClass!} ${properties.kcButtonPrimaryClass!} ${properties.kcButtonBlockClass!} ${properties.kcButtonLargeClass!}" name="login" id="kc-login" type="submit" value="${msg("doLogIn")}"/>
                    </div>
                </form>
            </div>
        </div>
    </#if>
    <script src="${url.resourcesPath}/../diam-corrections/js/script-20221128.js" defer></script>
    <style type="text/css"> <#include "css/styles-20221128.css"> </style>
    <style type="text/css"> <#include "css/bcsans-20221128.css"> </style>
</@layout.registrationLayout>