﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using BlazorBoilerplate.Shared.Interfaces;
using BlazorBoilerplate.Shared.Dto;
using BlazorBoilerplate.Shared.Extensions;
using System.Collections.Generic;
using BlazorBoilerplate.Shared.Dto.Account;
using Microsoft.JSInterop;
using System.Linq;
using System.Net;

using static Microsoft.AspNetCore.Http.StatusCodes;

namespace BlazorBoilerplate.Shared.Services
{
    public class AuthorizeApi : IAuthorizeApi
    {
        private readonly HttpClient _httpClient;
        private readonly NavigationManager _navigationManager;
        private readonly IJSRuntime _jsRuntime;

        public AuthorizeApi(NavigationManager navigationManager, HttpClient httpClient, IJSRuntime jsRuntime)
        {
            _navigationManager = navigationManager;
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
        }

        public async Task<ApiResponseDto> Login(LoginDto loginParameters)
        {
            ApiResponseDto resp;

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "api/Account/Login");
            httpRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(loginParameters));
            httpRequestMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            using (var response = await _httpClient.SendAsync(httpRequestMessage))
            {
                response.EnsureSuccessStatusCode();
//-:cnd:noEmit
#if ServerSideBlazor

                if (response.Headers.TryGetValues("Set-Cookie", out var cookieEntries))
                    foreach (var cookieEntry in cookieEntries)
                        await _jsRuntime.InvokeVoidAsync("cookieStorage.set", cookieEntry); //for security reasons this does not work with httponly cookie
#endif
//-:cnd:noEmit

                var content = await response.Content.ReadAsStringAsync();
                resp = JsonConvert.DeserializeObject<ApiResponseDto>(content);
            }

            return resp;
        }

        public async Task<ApiResponseDto> Logout()
        {
//-:cnd:noEmit
#if ServerSideBlazor
            List<string> cookies = null;
            if (_httpClient.DefaultRequestHeaders.TryGetValues("Cookie", out IEnumerable<string> cookieEntries))
                cookies = cookieEntries.ToList();
#endif
//-:cnd:noEmit

            var resp = await _httpClient.PostJsonAsync<ApiResponseDto>("api/Account/Logout", null);
//-:cnd:noEmit
#if ServerSideBlazor
            if (resp.StatusCode == Status200OK  && cookies != null && cookies.Any())
            {
                _httpClient.DefaultRequestHeaders.Remove("Cookie");

                foreach (var cookie in cookies[0].Split(';'))
                {
                    var cookieParts = cookie.Split('=');
                    await _jsRuntime.InvokeVoidAsync("cookieStorage.delete", cookieParts[0]);
                }
            }
#endif
//-:cnd:noEmit

            return resp;
        }

        public async Task<ApiResponseDto> Create(RegisterDto registerParameters)
        {
            return await _httpClient.PostJsonAsync<ApiResponseDto>("api/Account/Create", registerParameters);
        }

        public async Task<ApiResponseDto> Register(RegisterDto registerParameters)
        {
            return await _httpClient.PostJsonAsync<ApiResponseDto>("api/Account/Register", registerParameters);
        }

        public async Task<ApiResponseDto> ConfirmEmail(ConfirmEmailDto confirmEmailParameters)
        {
            return await _httpClient.PostJsonAsync<ApiResponseDto>("api/Account/ConfirmEmail", confirmEmailParameters);
        }

        public async Task<ApiResponseDto> ResetPassword(ResetPasswordDto resetPasswordParameters)
        {
            return await _httpClient.PostJsonAsync<ApiResponseDto>("api/Account/ResetPassword", resetPasswordParameters);
        }

        public async Task<ApiResponseDto> ForgotPassword(ForgotPasswordDto forgotPasswordParameters)
        {
            return await _httpClient.PostJsonAsync<ApiResponseDto>("api/Account/ForgotPassword", forgotPasswordParameters);
        }

        public async Task<UserInfoDto> GetUserInfo()
        {
            UserInfoDto userInfo = new UserInfoDto { IsAuthenticated = false, Roles = new List<string>() };

            var apiResponse = await _httpClient.GetNewtonsoftJsonAsync<ApiResponseDto<UserInfoDto>>("api/Account/UserInfo");

            if (apiResponse.StatusCode == Status200OK)
                userInfo = apiResponse.Result;

            return userInfo;
        }

        public async Task<UserInfoDto> GetUser()
        {
            var apiResponse = await _httpClient.GetNewtonsoftJsonAsync<ApiResponseDto<UserInfoDto>>("api/Account/GetUser");
            return apiResponse.Result;
        }

        public async Task<ApiResponseDto> UpdateUser(UserInfoDto userInfo)
        {
            return await _httpClient.PostJsonAsync<ApiResponseDto>("api/Account/UpdateUser", userInfo);
        }
    }
}