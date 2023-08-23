import axios, { type AxiosInstance } from "axios";

const apiClient: AxiosInstance = axios.create({
  baseURL: "http://localhost:7231/api",
  headers: {
    "Content-type": "application/json",
  },
});

export default apiClient;