import { injectable } from "inversify";
import { Dictionary } from "vue-router/types/router";

import RequestResult from "@/models/requestResult";
import UserFeedback from "@/models/userFeedback";
import { IHttpDelegate, IUserFeedbackService } from "@/services/interfaces";
import RequestResultUtil from "@/utility/requestResultUtil";

@injectable()
export class RestUserFeedbackService implements IUserFeedbackService {
    private readonly USER_FEEDBACK_BASE_URI: string = "v1/api/UserFeedback";
    private http!: IHttpDelegate;

    public initialize(http: IHttpDelegate): void {
        this.http = http;
    }

    public getFeedbackList(): Promise<UserFeedback[]> {
        return new Promise((resolve, reject) => {
            this.http
                .get<RequestResult<UserFeedback[]>>(
                    `${this.USER_FEEDBACK_BASE_URI}`
                )
                .then((requestResult) => {
                    return RequestResultUtil.handleResult(
                        requestResult,
                        resolve,
                        reject
                    );
                })
                .catch((err) => {
                    console.log(err);
                    return reject(err);
                });
        });
    }

    public toggleReviewed(feedback: UserFeedback): Promise<boolean> {
        return new Promise((resolve, reject) => {
            const headers: Dictionary<string> = {};
            headers["Content-Type"] = "application/json; charset=utf-8";
            this.http
                .patch<boolean>(
                    `${this.USER_FEEDBACK_BASE_URI}`,
                    JSON.stringify(feedback),
                    headers
                )
                .then((requestResult) => {
                    if (requestResult) {
                        resolve(true);
                    } else {
                        reject("Error toggling user feedback");
                    }
                })
                .catch((err) => {
                    console.log(err);
                    return reject(err);
                });
        });
    }
}
