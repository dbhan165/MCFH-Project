import { useState, type ElementType } from 'react';
import {
  Plug,
  CreditCard,
  Bell,
  Gauge,
  Eye,
  EyeOff,
  ChevronDown,
} from 'lucide-react';
import AdminLayout from '../../components/admin/AdminLayout';

type SettingsTab = 'api' | 'payment' | 'notifications' | 'thresholds';

interface SettingsCategory {
  id: SettingsTab;
  label: string;
  icon: ElementType;
}

const settingsCategories: SettingsCategory[] = [
  { id: 'api', label: 'API Integrations', icon: Plug },
  { id: 'payment', label: 'Payment Gateways', icon: CreditCard },
  { id: 'notifications', label: 'Notifications', icon: Bell },
  { id: 'thresholds', label: 'Global Thresholds', icon: Gauge },
];

const modelOptions = [
  'Gemini 1.5 Flash (Optimized Speed)',
  'Gemini 1.5 Pro (High Accuracy)',
  'Gemini 2.0 Flash (Latest)',
];

const SystemSettings = () => {
  const [activeTab, setActiveTab] = useState<SettingsTab>('api');
  const [showApiKey, setShowApiKey] = useState(false);
  const [apiKey, setApiKey] = useState('sk-gemini-xxxxxxxxxxxxxxxxxxxx');
  const [defaultModel, setDefaultModel] = useState(modelOptions[0]);
  const [dailyLimit, setDailyLimit] = useState('50,00');

  return (
    <AdminLayout
      searchPlaceholder="Search settings..."
      adminName="Admin User"
      adminRole="SUPER ADMIN"
    >
      <div className="mb-8">
        <h2 className="text-2xl lg:text-3xl font-bold tracking-tight">System Settings</h2>
        <p className="text-[#6b7280] text-sm mt-1">
          Configure global integrations, API thresholds, and system behaviors.
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-[280px_1fr] gap-6">
        <div className="space-y-2">
          {settingsCategories.map((category) => {
            const isActive = activeTab === category.id;
            return (
              <button
                key={category.id}
                type="button"
                onClick={() => setActiveTab(category.id)}
                className={`w-full flex items-center gap-3 px-4 py-3.5 rounded-xl text-sm font-medium transition-colors border ${
                  isActive
                    ? 'bg-red-50 text-[#ef4444] border-red-100'
                    : 'bg-white text-[#6b7280] border-gray-200 hover:bg-gray-50 hover:text-[#111827]'
                }`}
              >
                <category.icon className={`w-5 h-5 shrink-0 ${isActive ? 'text-[#ef4444]' : ''}`} />
                {category.label}
              </button>
            );
          })}
        </div>

        <div className="bg-white border border-gray-200 rounded-xl p-6 lg:p-8 shadow-sm">
          {activeTab === 'api' && (
            <>
              <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 mb-8">
                <div>
                  <h3 className="text-lg font-semibold text-[#111827]">Gemini API Settings</h3>
                  <p className="text-sm text-[#6b7280] mt-1">
                    Configure Large Language Model integration for data analysis.
                  </p>
                </div>
                <span className="inline-flex items-center text-xs font-bold uppercase tracking-wider px-3 py-1.5 rounded bg-blue-50 text-blue-600 shrink-0">
                  Active
                </span>
              </div>

              <form
                className="space-y-6"
                onSubmit={(e) => {
                  e.preventDefault();
                }}
              >
                <div>
                  <label className="block text-xs font-semibold text-[#6b7280] uppercase tracking-wider mb-2">
                    API Key
                  </label>
                  <div className="relative">
                    <input
                      type={showApiKey ? 'text' : 'password'}
                      value={apiKey}
                      onChange={(e) => setApiKey(e.target.value)}
                      className="w-full bg-gray-50 border border-gray-200 rounded-lg px-4 py-3 pr-12 text-sm text-[#111827] focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-400 transition-all"
                    />
                    <button
                      type="button"
                      onClick={() => setShowApiKey((prev) => !prev)}
                      className="absolute right-3 top-1/2 -translate-y-1/2 p-1.5 text-gray-400 hover:text-[#111827] rounded-lg transition-colors"
                    >
                      {showApiKey ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                    </button>
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
                  <div>
                    <label className="block text-xs font-semibold text-[#6b7280] uppercase tracking-wider mb-2">
                      Default Model
                    </label>
                    <div className="relative">
                      <select
                        value={defaultModel}
                        onChange={(e) => setDefaultModel(e.target.value)}
                        className="w-full appearance-none bg-gray-50 border border-gray-200 rounded-lg px-4 py-3 pr-10 text-sm text-[#111827] focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-400 transition-all"
                      >
                        {modelOptions.map((model) => (
                          <option key={model} value={model}>
                            {model}
                          </option>
                        ))}
                      </select>
                      <ChevronDown className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400 pointer-events-none" />
                    </div>
                  </div>

                  <div>
                    <label className="block text-xs font-semibold text-[#6b7280] uppercase tracking-wider mb-2">
                      Daily Usage Limit ($)
                    </label>
                    <input
                      type="text"
                      value={dailyLimit}
                      onChange={(e) => setDailyLimit(e.target.value)}
                      className="w-full bg-gray-50 border border-gray-200 rounded-lg px-4 py-3 text-sm text-[#111827] focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-400 transition-all"
                    />
                  </div>
                </div>

                <div className="flex justify-end pt-4">
                  <button
                    type="submit"
                    className="px-6 py-3 bg-[#ef4444] hover:bg-red-600 text-white rounded-lg text-sm font-semibold transition-colors shadow-sm"
                  >
                    Save API Configuration
                  </button>
                </div>
              </form>
            </>
          )}

          {activeTab === 'payment' && (
            <SettingsPlaceholder
              title="Payment Gateways"
              description="Configure Stripe, PayPal, and other payment provider credentials."
            />
          )}

          {activeTab === 'notifications' && (
            <SettingsPlaceholder
              title="Notification Settings"
              description="Manage email alerts, webhook endpoints, and system notification rules."
            />
          )}

          {activeTab === 'thresholds' && (
            <SettingsPlaceholder
              title="Global Thresholds"
              description="Set system-wide limits for scraping rate, API usage, and resource allocation."
            />
          )}
        </div>
      </div>
    </AdminLayout>
  );
};

const SettingsPlaceholder = ({
  title,
  description,
}: {
  title: string;
  description: string;
}) => (
  <div className="py-12 text-center">
    <h3 className="text-lg font-semibold text-[#111827] mb-2">{title}</h3>
    <p className="text-sm text-[#6b7280] max-w-md mx-auto">{description}</p>
    <p className="text-xs text-gray-400 mt-4">Coming soon in next release.</p>
  </div>
);

export default SystemSettings;
